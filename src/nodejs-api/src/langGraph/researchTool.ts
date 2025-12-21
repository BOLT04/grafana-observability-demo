import { tool } from "@langchain/core/tools";
import { z } from "zod";
import { withTool } from "@traceloop/node-server-sdk";

export const researchWebFakeTavify = tool(
  async (obj: any) => {
    return await withTool({ name: "researchWebFakeTavifyTool" }, () => {
      const responseTime = (Math.random() * 2.5).toFixed(2);
      const jsonResponse = `{
        "query": "${obj.input}",
        "answer": "Lionel Messi, born in 1987, is an Argentine footballer widely regarded as one of the greatest players of his generation. He spent the majority of his career playing for FC Barcelona, where he won numerous domestic league titles and UEFA Champions League titles. Messi is known for his exceptional dribbling skills, vision, and goal-scoring ability. He has won multiple FIFA Ballon d'Or awards, numerous La Liga titles with Barcelona, and holds the record for most goals scored in a calendar year. In 2014, he led Argentina to the World Cup final, and in 2015, he helped Barcelona capture another treble. Despite turning 36 in June, Messi remains highly influential in the sport.",
        "images": [],
        "results": [
            {
            "title": "Lionel Messi Facts | Britannica",
            "url": "https://www.britannica.com/facts/Lionel-Messi",
            "content": "Lionel Messi, an Argentine footballer, is widely regarded as one of the greatest football players of his generation. Born in 1987, Messi spent the majority of his career playing for Barcelona, where he won numerous domestic league titles and UEFA Champions League titles. Messi is known for his exceptional dribbling skills, vision, and goal",
            "score": 0.81025416,
            "raw_content": null,
            "favicon": "https://britannica.com/favicon.png"
            }
        ],
        "auto_parameters": {
            "topic": "general",
            "search_depth": "basic"
        },
        "response_time": "${responseTime}"
        }
      `;

      return jsonResponse;
    });
  },
  {
    name: "tavily_search_results_json",
    description:
      "A search engine optimized for comprehensive, accurate, and trusted results. Useful for when you need to answer questions about current events. Input should be a search query.",
    // @ts-ignore
    schema: z.object({
      input: z.string(),
    }),
  }
);

export const scrapeWebpage = tool(
  async (input: any) => {
    console.log("Scraping webpage with input:", input);
    return `<Document name="title">\npageContent with ${input.url}\n</Document>`;
  },
  {
    name: "scrape_webpage",
    description: "Scrape the contents of a webpage.",
    // @ts-ignore
    schema: z.object({
      url: z.string(),
    }),
  }
);
