using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBridge.MessageContent
{
    /// <summary>
    /// Represents question containing only common text
    /// </summary>
    public class PollContent : BridgeMessageContent
    {
        public readonly string Question;
        public readonly string[] Options;
        public readonly bool? IsAnonymous;
        public readonly bool? IsMultiple;

        /// <summary>
        /// Instantiates <seealso cref="BridgeMessageContent"/> with plain text as question
        /// </summary>
        /// <param name="question">Question text</param>
        /// <param name="options">Poll options</param>
        /// <param name="isAnonymous">If the poll is anonymous</param>
        /// <param name="isMultiple">If the poll allows multiple choice</param>
        public PollContent(string question, string[] options, bool? isAnonymous, bool? isMultiple) : base(BridgeMessageContentType.Poll)
        {
            Question = question;
            Options = options;
            IsAnonymous = isAnonymous;
            IsMultiple = isMultiple;
        }

        public override async Task<object> GetDataAsync()
        {
            await Task.CompletedTask;
            return this;
        }
    }
}