using System.Text;

namespace MicrosoftAgentFramework.Rag.Core.Application.Common.Builders;

/// <summary>
/// Concrete implementation of IPromptBuilder for RAG systems
/// </summary>
public class RAGPromptBuilder : IPromptBuilder
{
    private readonly StringBuilder _builder = new();
    private string? _systemMessage;
    private string? _context;
    private string? _question;
    private readonly List<string> _instructions = new();

    public IPromptBuilder WithSystemMessage(string systemMessage)
    {
        _systemMessage = systemMessage;
        return this;
    }

    public IPromptBuilder WithContext(string context)
    {
        _context = context;
        return this;
    }

    public IPromptBuilder WithQuestion(string question)
    {
        _question = question;
        return this;
    }

    public IPromptBuilder WithInstruction(string instruction)
    {
        _instructions.Add(instruction);
        return this;
    }

    public IPromptBuilder Clear()
    {
        _builder.Clear();
        _systemMessage = null;
        _context = null;
        _question = null;
        _instructions.Clear();
        return this;
    }

    public string Build()
    {
        _builder.Clear();

        if (!string.IsNullOrEmpty(_systemMessage))
        {
            _builder.AppendLine(_systemMessage);
            _builder.AppendLine();
        }

        if (!string.IsNullOrEmpty(_context))
        {
            _builder.AppendLine("Context Information:");
            _builder.AppendLine(_context);
            _builder.AppendLine();
        }

        if (_instructions.Count > 0)
        {
            _builder.AppendLine("Instructions:");
            foreach (var instruction in _instructions)
            {
                _builder.AppendLine($"- {instruction}");
            }
            _builder.AppendLine();
        }

        if (!string.IsNullOrEmpty(_question))
        {
            _builder.AppendLine($"Question: {_question}");
            _builder.AppendLine();
            _builder.AppendLine("Please answer the question based on the context provided above. If the context doesn't contain enough information, acknowledge this clearly.");
        }

        return _builder.ToString();
    }
}