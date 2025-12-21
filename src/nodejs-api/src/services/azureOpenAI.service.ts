import * as traceloop from "@traceloop/node-server-sdk";

import {
  AzureOpenAIGateway,
  OpenAIResponse,
} from "../gateways/azureOpenAI.gateway";

import { HumanMessage } from "@langchain/core/messages";
import { buildResearchChain } from "../langGraph/researchTeam";

export class AzureOpenAIService {
  private gateway: AzureOpenAIGateway;

  constructor() {
    this.gateway = new AzureOpenAIGateway();
  }

  // @traceloop.task({ name: "signature_generation" })
  @traceloop.workflow({ name: "my_generateCompletion" })
  async generateCompletion(query: string): Promise<OpenAIResponse> {
    return await this.gateway.generateCompletion(query);
  }

  @traceloop.workflow({ name: "agentResearchTest" })
  async agentResearchTest(query: string): Promise<void> {
    console.log("Start agent research");

    const researchChain = await buildResearchChain();
    const streamResults = researchChain.stream(
      {
        messages: [
          new HumanMessage(query),
          //new HumanMessage("who is messi and what is he doing?"),
        ],
      },
      { recursionLimit: 100 }
    );

    console.log("Streaming results...");
    for await (const output of await streamResults) {
      //@ts-ignore
      if (!output?.__end__) {
        console.log("output: ", output);
        console.log("----");
      } else {
        console.log("nothing");
      }
    }

    // TODO: much wow, maybe we have some bugs here
    // https://github.com/langchain-ai/langchainjs/pull/8366
    // https://github.com/langchain-ai/langchainjs/issues/7963
    // A wild bug in langchain js appeared: (node:19032) MaxListenersExceededWarning: Possible EventTarget memory leak detected. 11 abort listeners added to [AbortSignal]. Use events.setMaxListeners() to increase limit
    console.log("Finished agent research");
  }

  async isHealthy(): Promise<boolean> {
    return this.gateway.isHealthy();
  }
}
