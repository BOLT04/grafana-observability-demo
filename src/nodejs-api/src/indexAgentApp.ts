import './otelConfig';
import dotenv from 'dotenv';

import {
  ChatPromptTemplate,
  // HumanMessagePromptTemplate,
  // PromptTemplate,
  // SystemMessagePromptTemplate,
} from "@langchain/core/prompts";
import { buildNewAzureChatOpenAI } from "./common/azureOpenAILLM";
import { AzureOpenAIService } from './services/azureOpenAI.service';

dotenv.config();

(async () => {
  console.log("Loading Demo Agent App...");
  await indexAgentApp();

  //Uncomment to run research agent demo
  //await agentResearchDemo();
})();

//@ts-ignore
async function agentResearchDemo() {
  // Uncomment for write tool test
  //const result = await writeDocumentTool.invoke({
  //    content: "Hello from LangGraph!",
  //    file_name: "hello.txt",
  //});
  //console.log("Write Document Tool Response:", result);

  const openAIService = new AzureOpenAIService();
  const query = "What's the price of a big mac in Argentina?";
  await openAIService.agentResearchTest(query);
}

//@ts-ignore
async function indexAgentApp() {
  try {
    const prompt = "explain what is SRE in simple terms";
    if (!prompt) throw new Error("prompt data must be set in POST.");

    // LangChain LLM setup
    const llm = buildNewAzureChatOpenAI();

    const promptTemplate = ChatPromptTemplate.fromTemplate(
      "The following is a conversation with an AI assistant. The assistant is helpful.\n\n" +
        "A:How can I help you today?\n" +
        "Human: {human_prompt}?"
    );
    const formattedPrompt = await promptTemplate.format({
      human_prompt: prompt,
    });

    const response = await llm.invoke(formattedPrompt);
    console.log("Response:", response);
    console.log("Content:", response.content);
  } catch (err: any) {
    console.error("Got an error:", err.message);
    throw err;
  }
}
