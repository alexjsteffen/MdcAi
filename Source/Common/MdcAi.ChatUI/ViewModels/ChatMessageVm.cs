﻿namespace MdcAi.ChatUI.ViewModels;

using Windows.Storage;

using Markdig;
using System.Web;
using LocalDal;
using Mdc.OpenAiApi;

/// <summary>
/// This is a doubly linked list, plus each message can diverge and we keep all the versions.
/// </summary>
public class ChatMessageVm : ViewModel
{
    public string Id { get; set; }
    public string Role { get; set; }
    public ChatMessageSelectorVm Selector { get; }
    [Reactive] public ChatSettingsVm Settings { get; set; } = new();
    [Reactive] public string Content { get; set; }
    [Reactive] public string HTMLContent { get; set; }
    public DateTime CreatedTs { get; set; }
    public ConversationVm Conversation { get; }
    public ChatMessageVm Previous { get; set; } // Previous item        
    [Reactive] public ChatMessageVm Next { get; set; } // Next item
    [Reactive] public bool IsCompleting { get; private set; } // Whether completion is in progress

    public ReactiveCommand<Unit, string> CompleteCmd { get; }
    public ReactiveCommand<Unit, Unit> StopCompletionCmd { get; }

    public ChatMessageVm(ConversationVm conversation,
                         string role,
                         ChatMessageSelectorVm selector = null)
    {
        Conversation = conversation;
        CreatedTs = DateTime.Now;
        Role = role;
        Id = Guid.NewGuid().ToString();

        if (selector != null)
        {
            Selector = selector;
            Selector.Versions.Add(this);
        }
        else
            Selector = new(this);

        CompleteCmd = ReactiveCommand.CreateFromObservable(
            () => Observable.Return(Unit.Default)
                            .Do(_ => Content = null) // Just because there can be such a big delay when regenerating
                            .Select(_ => Settings.Streaming ?
                                        CreateGenerationStream()
                                            .TakeUntil(StopCompletionCmd)
                                            .Scan("", (a, b) => a + b) :
                                        Observable.FromAsync(async () => await GenerateResponse())
                                                  .TakeUntil(StopCompletionCmd))
                            .Switch());

        CompleteCmd.ObserveOnMainThread()
                   .Do(c => Content = c)
                   .Subscribe();

        StopCompletionCmd = ReactiveCommand.Create(() => { }, CompleteCmd.IsExecuting);

        CompleteCmd.IsExecuting
                   .ObserveOnMainThread()
                   .Do(i => IsCompleting = i)
                   .Subscribe();

        const string stopLabelHtml = "<span id=\"stop\"><b>Completion Stopped</b></stop>";
        const string caretMd = "'%caret%'";
        const string caretHtml = "<span id=\"caret\"/>";
        
        this.WhenAnyValue(vm => vm.Content)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOnMainThread()
            .Select(c =>
            {
                if (Role == ChatMessageRole.User)
                    return string.IsNullOrEmpty(c) ? "" : ToUserHtml(c);

                if (Next != null)
                    return string.IsNullOrEmpty(c) ? "" : Markdown.ToHtml(c);

                if (string.IsNullOrEmpty(c))
                    return caretHtml;

                // Hacky hack
                var actualCaretMd = caretMd;
                if (c.Trim().EndsWith("```"))
                    actualCaretMd = $"\r\n{caretMd}";

                var html = Markdown.ToHtml(c + actualCaretMd)
                                   .Replace(caretMd, caretHtml);

                return html;
            })
            .Do(h => HTMLContent = h)
            .Subscribe();

        this.WhenAnyValue(vm => vm.Content)
            .Throttle(TimeSpan.FromMilliseconds(2000))
            .ObserveOnMainThread()
            .Do(_ => HTMLContent = HTMLContent.Replace(caretHtml, ""))
            .Subscribe();

        StopCompletionCmd.ObserveOnMainThread()
                         .Do(_ =>
                         {
                             var html = HTMLContent;//.Replace(caretHtml, "");

                             if (string.IsNullOrEmpty(html))
                                 HTMLContent = stopLabelHtml;
                             else
                                 HTMLContent = html + "<br />" + stopLabelHtml;
                         })
                         .Subscribe();

        // Remove caret from the html altogether when done
        CompleteCmd.IsExecuting
                   .SkipWhile(i => !i)
                   .DistinctUntilChanged()
                   .Where(i => !i)
                   .Throttle(TimeSpan.FromMilliseconds(1000))
                   .ObserveOnMainThread()
                   .Do(_ => HTMLContent = HTMLContent.Replace(caretHtml, ""))
                   .Subscribe();
    }

    private static string ToUserHtml(string content) =>
        HttpUtility.HtmlEncode(content)
                   .Replace("\r", "<br />");

    private async Task<string> GenerateResponse()
    {
        if (Debugging.Enabled && Debugging.MockMessages)
        {
            await Task.Delay(1000);

            if (Debugging.NumberedMessages)
                return $"Debug system {Debugging.SystemMessageCounter++}";

            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Dbg/Test3.md"));
            var contents = await FileIO.ReadTextAsync(file);
            return contents;
        }

        var req = CreateRequest();
        var completions = await Conversation.Api.CreateChatCompletions(req);
        return completions.Choices.LastOrDefault()?.Message.Content;
    }

    private IObservable<string> CreateGenerationStream()
    {
        if (Debugging.Enabled && Debugging.MockMessages)
            return Observable
                   .FromAsync(async () => await GenerateResponse())
                   .SelectMany(c => c.Split(' ')
                                     .ToObservable()
                                     .Select(s => Observable.Timer(TimeSpan.FromMilliseconds(200))
                                                            .Select(_ => s + ' '))
                                     .Concat());

        return Conversation.Api.CreateChatCompletionsStream(CreateRequest())
                           .ToObservable()
                           .Select(m => m.Choices.LastOrDefault()?.Delta.Content);
    }

    private ChatRequest CreateRequest()
    {
        var messages = new List<ChatMessage>();
        var currentParent = Previous;

        while (currentParent != null)
        {
            messages.Insert(0, currentParent.CreateMessageRequest());
            currentParent = currentParent.Previous;
        }

        messages.Insert(0,
                        new()
                        {
                            Role = ChatMessageRole.System,
                            Content = Settings.Premise
                        });

        var req = new ChatRequest
        {
            Messages = messages,
            Model = Settings.Model
        };

        return req;
    }

    private ChatMessage CreateMessageRequest() =>
        new()
        {
            Content = Content,
            Role = Role
        };
}