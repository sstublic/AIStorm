1. ++Simplify MarkdownStorage code. Remove checks for extensions, everything is WITHOUT extension. Remove deprecated methods.
2. Make Session properties immutable, prevent instantiation and then modifying agents and messages directly. Agents are immutable.
3. Premise is incorrectly added at the end of messages instead of at start
4. Move creating message sequence logic from OpenAIProvider into generic (use System, Assistant,User)
5. SessionRunner constructor is suspiciously redundant with Session creation
6. 