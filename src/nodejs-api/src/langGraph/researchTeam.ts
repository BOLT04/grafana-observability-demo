import { BaseMessage } from "@langchain/core/messages";
import { Annotation, END, START, StateGraph } from "@langchain/langgraph";
import { createReactAgent } from "@langchain/langgraph/prebuilt";
import { buildNewAzureChatOpenAI } from "../common/azureOpenAILLM";
import {
  agentStateModifier,
  createTeamSupervisor,
  runAgentNode,
} from "./agentUtilities";
import { researchWebFakeTavify, scrapeWebpage } from "./researchTool";

// TODO: refactor out of this file the annotations
import { withAgent } from "@traceloop/node-server-sdk";

const ResearchTeamState = Annotation.Root({
  messages: Annotation<BaseMessage[]>({
    reducer: (x, y) => x.concat(y),
  }),
  team_members: Annotation<string[]>({
    reducer: (x, y) => x.concat(y),
  }),
  next: Annotation<string>({
    reducer: (x, y) => y ?? x,
    default: () => "supervisor",
  }),
  instructions: Annotation<string>({
    reducer: (x, y) => y ?? x,
    default: () => "Solve the human's question.",
  }),
});

const llm = buildNewAzureChatOpenAI();

export const searchNode = async (state: typeof ResearchTeamState.State) => {
  return await withAgent({ name: "searchNodeAgent" }, () => {
    console.log("APP: searchNode ");

    const stateModifier = agentStateModifier(
      "You are a research assistant who can search for up-to-date info using the tavily search engine.",
      [researchWebFakeTavify],
      state.team_members ?? ["Search"]
    );
    const searchAgent = createReactAgent({
      llm,
      tools: [researchWebFakeTavify],
      stateModifier,
    });

    console.log("Running search agent");
    return runAgentNode({ state, agent: searchAgent, name: "Search" });
  });
};

export const researchNode = async (state: typeof ResearchTeamState.State) => {
  return await withAgent({ name: "researchNodeAgent" }, () => {
    console.log("APP: researchNode ");

    const stateModifier = agentStateModifier(
      "You are a research assistant who can scrape specified urls for more detailed information using the scrapeWebpage function.",
      [scrapeWebpage],
      state.team_members ?? ["WebScraper"]
    );
    const researchAgent = createReactAgent({
      llm,
      tools: [scrapeWebpage],
      stateModifier,
    });

    console.log("Running research agent");
    return runAgentNode({ state, agent: researchAgent, name: "WebScraper" });
  });
};

export async function createResearchTeamSupervisor() {
  const supervisorAgent = await createTeamSupervisor(
    llm,
    "You are a supervisor tasked with managing a conversation between the" +
      " following workers:  {team_members}. Given the following user request," +
      " respond with the worker to act next. Each worker will perform a" +
      " task and respond with their results and status. When finished," +
      " respond with FINISH.\n\n" +
      " Select strategically to minimize the number of steps taken.",
    ["Search", "WebScraper"]
  );

  return supervisorAgent;
}

export async function buildResearchChain() {
  const supervisorAgent = await createResearchTeamSupervisor();
  const researchGraph = new StateGraph(ResearchTeamState)
    .addNode("Search", searchNode)
    .addNode("supervisor", supervisorAgent)
    .addNode("WebScraper", researchNode)
    // Define the control flow
    .addEdge("Search", "supervisor")
    .addEdge("WebScraper", "supervisor")
    .addConditionalEdges("supervisor", (x) => x.next, {
      Search: "Search",
      WebScraper: "WebScraper",
      FINISH: END,
    })
    .addEdge(START, "supervisor");

  return researchGraph.compile();
}
