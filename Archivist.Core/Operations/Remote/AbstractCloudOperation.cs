using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Archivist.Core.Operations.Remote
{
    public abstract class AbstractCloudOperation : AbstractOperation
    {
        protected AbstractCloudOperation(string connectionString, string containerName, Guid id) : base(id)
        {
            ConnectionString = connectionString;
            ContainerName = containerName;
        }

        protected string ConnectionString { get; }
        protected string ContainerName { get; }

        protected CloudBlobContainer GetContainer()
        {
            var account = CloudStorageAccount.Parse(ConnectionString);
            var client = account.CreateCloudBlobClient();
            return client.GetContainerReference(ContainerName);
        }

        protected void RunOperations<T>(IEnumerable<T> operationSource, int maxConcurency, Func<T, Task> factory, Util.Environment env)
        {
            var currentTasks = new List<Task<T>>();
            var currentRetries = new List<T>();
            var operations = new Queue<T>(operationSource);
            var total = operations.Count;
            var counter = 0;
            env.ReportStatus(Id, OperationStatus.InProgress);
            env.ReportProgress(Id, counter, total);
            bool autoRetry = false;
            while (counter < total)
            {
                if (currentRetries.Count == 0)
                {
                    if (operations.Count > 0 && currentTasks.Count < maxConcurency)
                    {
                        var operationToGo = operations.Dequeue();
                        var task = Task.Run(() => RunTask(operationToGo, factory));
                        currentTasks.Add(task);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }

                if (currentRetries.Count >= maxConcurency)
                {
                    bool? currentRetry = null;
                    if (false == autoRetry)
                    {
                        var askRetry = AskRetry(currentRetries.Count, env);
                        switch (askRetry)
                        {
                            case RetryAsnwer.Yes:
                                currentRetry = true;
                                break;
                            case RetryAsnwer.No:
                                currentRetry = false;
                                break;
                            case RetryAsnwer.Auto:
                                autoRetry = true;
                                currentRetry = true;
                                break;
                            default:
                                autoRetry = false;
                                currentRetry = null;
                                break;
                        }
                    }
                    else
                    {
                        currentRetry = true;
                        env.WriteOut($"{currentRetries.Count} operations will be auto-retried");
                    }


                    if (currentRetry == true)
                    {
                        foreach (var retry in currentRetries)
                        {
                            operations.Enqueue(retry);
                        }
                        currentRetries.Clear();
                    }
                    else if (currentRetry == null)
                    {
                        env.ReportStatus(Id, OperationStatus.Canceled);
                        return;
                    }
                    else
                    {
                        currentRetries.Clear();
                    }
                }
                else
                {
                    currentTasks.RemoveAll(_ =>
                    {
                        if (_.IsCompleted)
                        {
                            if (EqualityComparer<T>.Default.Equals(default(T), _.Result))
                            {
                                counter++;
                                env.ReportProgress(Id, counter, total);
                            }
                            else
                            {
                                currentRetries.Add(_.Result);
                            }
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
            }

            env.ReportStatus(Id, OperationStatus.Success);
        }

        private async Task<T> RunTask<T>(T input, Func<T, Task> factory)
        {
            try
            {
                await factory(input);
            }
            catch
            {
                return input;
            }
            return default(T);
        }

        private RetryAsnwer AskRetry(int retriesCount, Util.Environment env)
        {
            env.WriteOut($"{retriesCount} operations failed. Do you want to retry?[Y]es/[N]o/[C]ancel/[A]uto retry always");
            var key = env.GetKey();
            switch (key.Key)
            {
                case ConsoleKey.Y:
                    return RetryAsnwer.Yes;
                case ConsoleKey.C:
                    return RetryAsnwer.Cancel;
                case ConsoleKey.A:
                    return RetryAsnwer.Auto;
                default:
                    return RetryAsnwer.No;
            }
        }

        private enum RetryAsnwer
        {
            Yes,
            No,
            Cancel,
            Auto
        }
    }
}
