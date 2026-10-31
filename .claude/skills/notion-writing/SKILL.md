---
name: notion-writing
description: Write or update this project's Notion pages (art prototype notes, references, plans). Use when the user asks to write, add, save or update something "in Notion", or mentions the Art Prototype page. Holds the page links and the formatting rules that the Notion connector needs.
---

# Writing to Notion

Write through the Notion connector tools (`mcp__claude_ai_Notion__*`). Load them with ToolSearch first, for example `select:mcp__claude_ai_Notion__notion-fetch,mcp__claude_ai_Notion__notion-update-page,mcp__claude_ai_Notion__notion-create-pages`. No CLI or API token is needed.

## The project's pages

| Page | Link | What goes there |
| --- | --- | --- |
| LLM in game industry | https://app.notion.com/p/4098c134506e8062acd1e7cce474f789 | Top-level workspace page. Don't write here unless asked. |
| Prototyping the art | https://app.notion.com/p/40a8c134506e80bd9770e97848628038 | Parent page for the art work. New art pages go under it. |
| Art Prototype | https://app.notion.com/p/40a8c134506e80f0b8ddd6d45ea4300d | Reference images (5 pixel art, 5 low-poly) and art direction notes. |

If the user names a page that isn't in this table, ask for its link. When a new page is created, add it to the table.

## Art direction so far

- The user chose the **Lil Gator Game** look: toon lighting, outlines, bold warm colours, chunky simple shapes.
- The game uses a 3D scene with a camera looking down from above, so the style has to read from high up.

Update this section when the direction changes.

## How to write

1. **Fetch the page first** (`notion-fetch`) so you know what's already there.
2. **Change as little as possible.** To add something, use `insert_content`, which goes at the end by default. To change a part, use `update_content` with search-and-replace. Use `replace_content` only on an empty page, or when the user asks for a rewrite.
3. **Never delete what the user wrote** without asking.
4. To make a new page, create it under the right parent from the table above.

## Formatting rules

- **Images must be their own block**, with a blank line before and after: `![caption](url)`. If an image shares a line with text, the connector rejects the whole write.
- For images from the web, link a stable source, such as official Steam store screenshots. Get them from `https://store.steampowered.com/api/appdetails?appids=<id>&filters=screenshots`, then use `path_full`. Look at an image before using it.
- Layout: a one-line intro, `#` sections, `##` items, short **bold-labelled** notes. Add a source link for anything taken from the web.
- Write plainly, in short sentences. Don't add filler.

## After writing

Give the user the page link and say in one or two lines what changed.
