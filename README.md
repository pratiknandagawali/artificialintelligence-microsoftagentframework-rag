Microsoft Agent Framework - RAG Demo

Simple demo application showing Retrieval-Augmented Generation (RAG) patterns using Microsoft Semantic Kernel and Azure OpenAI.

Prerequisites

- .NET 8 SDK installed
- An Azure OpenAI resource (or equivalent) with Chat and Embedding deployments
- (Optional) Azure Cosmos DB if you want persistent vector storage

Configuration

The app reads configuration from `microsoftagentframework.rag/appsettings.json`. Update these sections with your values:

- `AzureOpenAI`:
  - `ChatEndpoint` — chat endpoint URL
  - `ChatApiKey` — chat API key
  - `ChatDeploymentName` — chat deployment name (e.g., `gpt-5-chat`)
  - `EmbeddingEndpoint` — embedding endpoint URL
  - `EmbeddingApiKey` — embedding API key
  - `EmbeddingDeploymentName` — embedding deployment name (e.g., `text-embedding-3-large`)

You can also set these values via environment variables (same names) if you prefer not to store secrets in `appsettings.json`.

- `VectorStore`:
  - `Provider` — `InMemory` (default) or `CosmosDB`
  - `CosmosDB` (when using `Provider: CosmosDB`):
    - `Endpoint`, `Key`, `DatabaseName`, `ContainerName`, `VectorDimensions`
  - `Chunking`:
    - `ChunkSize` and `ChunkOverlap`

Example minimal `appsettings.json` (already present in the repo):

- `microsoftagentframework.rag/appsettings.json` contains placeholders. Replace them with your real endpoints/keys.

Run the project

From repository root:

- `dotnet run --project microsoftagentframework.rag`

Or change directory into the project folder and run:

- `cd microsoftagentframework.rag`
- `dotnet run`

What the app does

- Loads sample documents and indexes them (in memory or Cosmos DB depending on config)
- Interactive console to ask questions using Traditional RAG or Agentic RAG
- Options to add documents and run filtered searches

Notes

- For development the code disables strict TLS validation in a couple of HttpClient handlers — do not use that in production.
- If you choose `CosmosDB` as `VectorStore:Provider`, ensure the `CosmosDB` config is filled and the account supports the chosen vector dimensions.

If you need a more detailed setup (CI, Docker, or Azure deployment), ask and a brief guide can be added.