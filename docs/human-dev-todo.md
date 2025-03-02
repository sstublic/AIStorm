1. ++Simplify MarkdownStorage code. Remove checks for extensions, everything is WITHOUT extension. Remove deprecated methods.
2. Make Session properties immutable, prevent instantiation and then modifying agents and messages directly. Agents are immutable.
3. Premise is incorrectly added at the end of messages instead of at start
4. 