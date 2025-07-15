---
mode: 'agent'
model: Claude Sonnet 4
description: 'refactor deployment-prompts.md to individual prompt files'
---

extract the prompts from the deployment-prompts.md file into individual prompt files into the .github/prompts folder. Use this file structure:

Prompt file structure
A prompt file is a Markdown file with the .prompt.md file suffix. It has the following two main sections:

(Optional) Header with metadata (Front Matter syntax)

mode: The chat mode to use when running the prompt: ask, edit, or agent (default).
model: The AI model to use when running the prompt. If not specified, the currently selected model in model picker is used.
tools: Array of tool (set) names to indicate which tools (sets) can be used in agent mode. Select Configure Tools to select the tools from the list of available tools in your workspace. If a given tool (set) is not available when running the prompt, it is ignored.
description: A short description of the prompt.
Body with the prompt content

Prompt files mimic the format of writing prompts in chat. This allows blending natural language instructions, additional context, and even linking to other prompt files as dependencies. You can use Markdown formatting to structure the prompt content, including headings, lists, and code blocks.

You can reference other workspace files, prompt files, or instructions files by using Markdown links. Use relative paths to reference these files, and ensure that the paths are correct based on the location of the prompt file.

replace the prompt text in the deployment-prompts.md file with links to the new prompt files.

Include the model declaration for Claude Sonnet 4.

Include a tools declaration for any required tools.