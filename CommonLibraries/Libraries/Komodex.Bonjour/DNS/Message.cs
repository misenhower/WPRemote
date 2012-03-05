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
        public Message()
        { }

        public Message(byte[] bytes, int index, int count)
            : this()
        {
            ParseBytes(bytes, index, count);
        }

        #region Fields

        private IList<Question> _questions = new List<Question>();
        private IList<ResourceRecord> _answerRecords = new List<ResourceRecord>();
        private IList<ResourceRecord> _authorityRecords = new List<ResourceRecord>();
        private IList<ResourceRecord> _additionalRecords = new List<ResourceRecord>();

        #endregion

        #region Properties

        /// <summary>
        /// Transaction ID (ID)
        /// </summary>
        public ushort TransactionID { get; set; }

        /// <summary>
        /// Query/Response (QR) Bit. False if this is a query, true if this is a response.
        /// Bit 15 in the message flags. 
        /// </summary>
        public bool QueryResponse { get; set; }

        /// <summary>
        /// OPCODE. Must be zero on both queries and responses.
        /// Bits 11-14 in the message flags.
        /// </summary>
        public byte Opcode { get; set; }

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
        public byte ResponseCode { get; set; }

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

        protected void ParseBytes(byte[] bytes, int index, int count)
        {
            
        }

        public byte[] GetBytes()
        {
            List<byte> result = new List<byte>(1024);

            // ID
            result.AddNetworkOrderBytes(TransactionID);
            System.Diagnostics.Debug.WriteLine(result.Count);

            // Flags
            // QR, OPCODE, AA, TC, RD
            byte flags = 0;
            // QR
            if (QueryResponse) flags = (byte)(flags | (1 << 7));
            // OPCODE
            flags = (byte)(flags | ((Opcode & 0x0f) << 3));
            // AA
            if (AuthoritativeAnswer) flags = (byte)(flags | (1 << 2));
            // TC
            if (Truncated) flags = (byte)(flags | (1 << 1));
            // RD
            if (RecursionDesired) flags = (byte)(flags | (1 << 0));
            result.Add(flags);

            // RA, Z, AD, CD, RCODE
            flags = 0;
            // RA
            if (RecursionAvailable) flags = (byte)(flags | (1 << 7));
            // Z
            // (zero bit)
            // AD
            if (AuthenticData) flags = (byte)(flags | (1 << 5));
            // CD
            if (Truncated) flags = (byte)(flags | (1 << 4));
            // RCODE
            flags = (byte)(flags | ((ResponseCode & 0x0f) << 0));
            result.Add(flags);

            // Number of Questions
            result.AddNetworkOrderBytes((ushort)Questions.Count);
            // Number of Answers
            result.AddNetworkOrderBytes((ushort)AnswerRecords.Count);
            // Number of Authority Records
            result.AddNetworkOrderBytes((ushort)AuthorityRecords.Count);
            // Number of Additional Records
            result.AddNetworkOrderBytes((ushort)AdditionalRecords.Count);

            // Message Content
            // Questions
            for (int i = 0; i < Questions.Count; i++)
                result.AddRange(Questions[i].GetBytes());

            // TODO: Answers, Authority Records, and Additional Records

            return result.ToArray();
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
