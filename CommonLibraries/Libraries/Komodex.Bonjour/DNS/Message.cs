using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;
using System.Collections.Generic;

namespace Komodex.Bonjour.DNS
{
    internal class Message
    {
        #region Fields

        private IList<Question> _questions = new List<Question>();
        private IList<ResourceRecord> _answerRecords = new List<ResourceRecord>();
        private IList<ResourceRecord> _authorityRecords = new List<ResourceRecord>();
        private IList<ResourceRecord> _additionalRecords = new List<ResourceRecord>();

        #endregion

        #region Properties

        /// <summary>
        /// Query Identifier
        /// </summary>
        public int QueryIdentifier { get; set; }

        /// <summary>
        /// Query/Response (QR) Bit. False if this is a query, true if this is a response.
        /// Bit 15 in the message flags. 
        /// </summary>
        public bool QueryResponse { get; set; }

        /// <summary>
        /// OPCODE. Must be zero on both queries and responses.
        /// Bits 11-14 in the message flags.
        /// </summary>
        public int Opcode { get; set; }

        /// <summary>
        /// Authoritative Answer (AA) bit. Must be zero on transmission.
        /// Bit 10 in the message flags.
        /// </summary>
        public bool AuthoritativeAnswer { get; set; }

        /// <summary>
        /// Truncated (TC) bit.
        /// Bit 9 in the message flags.
        /// </summary>
        public bool Truncated { get; set; }

        /// <summary>
        /// Recursion Desired (RD) bit. Should be zero on transmission.
        /// Bit 8 in the message flags.
        /// </summary>
        public bool RecursionDesired { get; set; }

        /// <summary>
        /// Recursion Available (RA) bit.
        /// Bit 7 in the message flags.
        /// </summary>
        public bool RecursionAvailable { get; set; }

        /// <summary>
        /// Authentic Data (AD) bit.  Must be zero on transmission.
        /// Bit 5 in the message flags.
        /// </summary>
        public bool AuthenticData { get; set; }

        /// <summary>
        /// Checking Disabled (CD) bit. Must be zero on transmission.
        /// Bit 4 in the message flags.
        /// </summary>
        public bool CheckingDisabled { get; set; }

        /// <summary>
        /// Response Code (RCODE). Responses received with non-zero Response Codes must be silently ignored.
        /// Bits 0-3 in the message flags.
        /// </summary>
        public int ResponseCode { get; set; }

        /// <summary>
        /// Questions included in this message.
        /// </summary>
        public IList<Question> Questions { get { return _questions; } }

        /// <summary>
        /// Answers included in this message.
        /// </summary>
        public IList<ResourceRecord> AnswerRecords { get { return _answerRecords; } }

        /// <summary>
        /// Name server authority records included in this message.
        /// </summary>
        public IList<ResourceRecord> AuthorityRecords { get { return _authorityRecords; } }

        /// <summary>
        /// Additional records included in this message.
        /// </summary>
        public IList<ResourceRecord> AdditionalRecords { get { return _additionalRecords; } }

        #endregion

        #region Methods

        public byte[] GetBytes()
        {
            return new byte[0];
        }

        public override string ToString()
        {
            string result = "DNS ";
            result += (QueryResponse) ? "Response" : "Query";
            result += ": ";
            result += string.Format("Questions: {0}, Answer RRs: {1}, Authority RRs: {2}, Additional RRs: {3}", Questions.Count, AnswerRecords.Count, AuthorityRecords.Count, AdditionalRecords.Count);
            return result;
        }

        #endregion
    }
}
