Simple command line tool to work with Azure Blob storage, including Archive tier.

To run program, create config.json with following schema:
{
  "ConnectionString": "", // connection string to storage account in Azure
  "ContainerName":  "" // Block blob container name to work with
  "BasePath": "" // optional base path
}