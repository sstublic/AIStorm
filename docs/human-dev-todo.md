1. [x] Simplify MarkdownStorage code. Remove checks for extensions, everything is WITHOUT extension. Remove deprecated methods.
2. [x] Make Session properties immutable, prevent instantiation and then modifying agents and messages directly. Agents are immutable.
3. [x] Premise is incorrectly added at the end of messages instead of at start
4. [x] Move creating message sequence logic from OpenAIProvider into generic (use System, Assistant,User)
5. [x] SessionRunner constructor is suspiciously redundant with Session creation
6. [x] PromptBuilder extensive test, options for format text?
7. [ ] Make all props in Models readonly
8.
