import { tool } from "@langchain/core/tools";
import { z } from "zod";

export const createOutlineTool = tool(
  //@ts-ignore
  async ({ points, file_name }) => {

    return `Outline saved to ${file_name} with points: ${points.join(", ")}`;
  },
  {
    name: "create_outline",
    description: "Create and save an outline.",
    schema: z.object({
      points: z
        .array(z.string())
        .nonempty("List of main points or sections must not be empty."),
      file_name: z.string(),
    }),
  }
);

export const readDocumentTool = tool(
  //@ts-ignore
  async ({ file_name, start, end }) => {
    await new Promise((resolve) => setTimeout(resolve, 2130));

    return 'my lines of document';
  },
  {
    name: "read_document",
    description: "Read the specified document.",
    schema: z.object({
      file_name: z.string(),
      start: z.number().optional(),
      end: z.number().optional(),
    }),
  }
);

export const writeDocumentTool = tool(
  //@ts-ignore
  async (input: { content: string; file_name: string }): string => {
    const { content, file_name } = input;
    await new Promise((resolve) => setTimeout(resolve, 1130));

    return `Document saved to ${file_name}, with content: ${content}`;
  },
  {
    name: "write_document",
    description: "Create and save a text document.",
    schema: z.object({
      content: z.string(),
      file_name: z.string(),
    }),
  }
);

export const editDocumentTool = tool(
  //@ts-ignore
  async ({ file_name, inserts }) => {
    await new Promise((resolve) => setTimeout(resolve, 1000));

    return `Document edited and saved to ${file_name}`;
  },
  {
    name: "edit_document",
    description: "Edit a document by inserting text at specific line numbers.",
    schema: z.object({
      file_name: z.string(),
      inserts: z.record(z.number(), z.string()),
    }),
  }
);

export const chartTool = tool(
  async () => {
    return "Chart has been generated and displayed to the user!";
  },
  {
    name: "generate_bar_chart",
    description:
      "Generates a bar chart from an array of data points using D3.js and displays it for the user."
  }
);
