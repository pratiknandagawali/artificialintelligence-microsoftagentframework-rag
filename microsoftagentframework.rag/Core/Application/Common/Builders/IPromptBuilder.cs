namespace MicrosoftAgentFramework.Rag.Core.Application.Common.Builders;

/// <summary>
/// Builder interface for constructing prompts
/// Follows Builder Pattern to separate construction from representation
/// </summary>
public interface IPromptBuilder
{
    IPromptBuilder WithSystemMessage(string systemMessage);
    IPromptBuilder WithContext(string context);
    IPromptBuilder WithQuestion(string question);
    IPromptBuilder WithInstruction(string instruction);
    IPromptBuilder Clear();
    string Build();
}