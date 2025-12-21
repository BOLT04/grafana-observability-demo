import OpenAI from 'openai';

export interface OpenAIResponse {
  content: string;
  usage: {
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
  } | undefined;
  model: string;
  finishReason: string;
}

export class AzureOpenAIGateway {
  private client: OpenAI;
  private deploymentName: string;

  constructor() {
    const endpoint = process.env.AZURE_OPENAI_ENDPOINT;
    const apiKey = process.env.AZURE_OPENAI_API_KEY;
    const apiVersion = process.env.AZURE_OPENAI_API_VERSION || '2024-02-15-preview';
    this.deploymentName = process.env.AZURE_OPENAI_DEPLOYMENT_NAME || 'gpt-35-turbo';

    if (!endpoint || !apiKey) {
      throw new Error('Azure OpenAI configuration is missing. Please set AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_API_KEY environment variables.');
    }

    this.client = new OpenAI({
      apiKey: apiKey,
      baseURL: `${endpoint}/openai/deployments/${this.deploymentName}`,
      defaultQuery: { 'api-version': apiVersion },
      defaultHeaders: {
        'api-key': apiKey,
      },
    });
  }

  async generateCompletion(query: string): Promise<OpenAIResponse> {
    try {
      const response = await this.client.chat.completions.create({
        model: this.deploymentName,
        messages: [
          {
            role: 'system',
            content: 'You are a helpful AI assistant. Provide accurate and concise responses to user queries.'
          },
          {
            role: 'user',
            content: query
          }
        ],
        //max_tokens: 1000,
        //temperature: 0.7,
        //top_p: 0.95,
        frequency_penalty: 0,
        presence_penalty: 0
      });

      const choice = response.choices[0];
      if (!choice || !choice.message) {
        throw new Error('No response received from Azure OpenAI');
      }

      return {
        content: choice.message.content || '',
        usage: response.usage ? {
          promptTokens: response.usage.prompt_tokens,
          completionTokens: response.usage.completion_tokens,
          totalTokens: response.usage.total_tokens
        } : undefined,
        model: response.model || this.deploymentName,
        finishReason: choice.finish_reason || 'unknown'
      };
    } catch (error) {
      console.error('Azure OpenAI API error:', error);
      throw new Error(`Failed to generate completion: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }

  async isHealthy(): Promise<boolean> {
    try {
      // Simple health check with a minimal request
      const response = await this.client.chat.completions.create({
        model: this.deploymentName,
        messages: [{ role: 'user', content: 'Hello' }],
        max_tokens: 1
      });
      return response.choices.length > 0;
    } catch (error) {
      console.error('Azure OpenAI health check failed:', error);
      return false;
    }
  }
}
