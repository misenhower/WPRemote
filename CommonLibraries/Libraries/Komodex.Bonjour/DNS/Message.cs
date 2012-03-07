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
using System.IO;
using Komodex.Common;

namespace Komodex.Bonjour.DNS
{
    internal class Message
    {
        public Message()
        { }

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
        public int TransactionID { get; set; }

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

        public static Message FromBytes(byte[] bytes, int index, int count)
        {
            Message message = new Message();

            using (MemoryStream stream = new MemoryStream(bytes, index, count))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // ID
                message.TransactionID = reader.ReadNetworkOrderUInt16();

                // Flags
                // QR, OPCODE, AA, TC, RD
                byte flags = reader.ReadByte();
                // QR
                message.QueryResponse = flags.GetBit(7);
                // OPCODE
                message.Opcode = (byte)((flags >> 3) & 0x0f);
                // AA
                message.AuthoritativeAnswer = flags.GetBit(2);
                // TC
                message.Truncated = flags.GetBit(1);
                // RD
                message.RecursionDesired = flags.GetBit(0);

                // RA, Z, AD, CD, RCODE
                flags = reader.ReadByte();
                // RA
                message.RecursionAvailable = flags.GetBit(7);
                // Z
                // (zero bit)
                // AD
                message.AuthenticData = flags.GetBit(5);
                // CD
                message.CheckingDisabled = flags.GetBit(4);
                // RCODE
                message.ResponseCode = (byte)(flags & 0x0f);

                // Number of questions and answer records
                ushort questions, answers, authorityRecords, additionalRecords;

                // Number of Questions
                questions = reader.ReadNetworkOrderUInt16();
                // Number of Answers
                answers = reader.ReadNetworkOrderUInt16();
                // Number of Authority Records
                authorityRecords = reader.ReadNetworkOrderUInt16();
                // Number of Additional Records
                additionalRecords = reader.ReadNetworkOrderUInt16();

                // Read each Question and Resource Record
                // Questions
                while (questions-- > 0)
                    message.Questions.Add(Question.FromBinaryReader(reader));
                // Answers
                while (answers-- > 0)
                    message.AnswerRecords.Add(ResourceRecord.FromBinaryReader(reader));
                // Authority Records
                while (authorityRecords-- > 0)
                    message.AnswerRecords.Add(ResourceRecord.FromBinaryReader(reader));
                // Additional Records
                while (additionalRecords-- > 0)
                    message.AdditionalRecords.Add(ResourceRecord.FromBinaryReader(reader));
            }

            return message;
        }

        public byte[] GetBytes()
        {
            List<byte> result = new List<byte>(1024);

            // ID
            result.AddNetworkOrderBytes((ushort)TransactionID);

            // Flags
            // QR, OPCODE, AA, TC, RD
            byte flags = 0;
            // QR
            Utility.SetBit(ref flags, 7, QueryResponse);
            // OPCODE
            flags = (byte)(flags | ((Opcode & 0x0f) << 3));
            // AA
            Utility.SetBit(ref flags, 2, AuthoritativeAnswer);
            // TC
            Utility.SetBit(ref flags, 1, Truncated);
            // RD
            Utility.SetBit(ref flags, 0, RecursionDesired);
            result.Add(flags);

            // RA, Z, AD, CD, RCODE
            flags = 0;
            // RA
            Utility.SetBit(ref flags, 7, RecursionAvailable);
            // Z
            // (zero bit)
            // AD
            Utility.SetBit(ref flags, 5, AuthenticData);
            // CD
            Utility.SetBit(ref flags, 4, CheckingDisabled);
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
            // Answers
            for (int i = 0; i < AnswerRecords.Count; i++)
                result.AddRange(AnswerRecords[i].GetBytes());
            // Authority Records
            for (int i = 0; i < AuthorityRecords.Count; i++)
                result.AddRange(AuthorityRecords[i].GetBytes());
            // Additional Records
            for (int i = 0; i < AdditionalRecords.Count; i++)
                result.AddRange(AdditionalRecords[i].GetBytes());

            return result.ToArray();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("DNS ");
            sb.Append((QueryResponse) ? "Response" : "Query");
            sb.Append(": ");
            sb.AppendFormat("(Questions: {0}, Answer RRs: {1}, Authority RRs: {2}, Additional RRs: {3})", Questions.Count, AnswerRecords.Count, AuthorityRecords.Count, AdditionalRecords.Count);
            sb.AppendLine();
            sb.AppendFormat("OPCODE: {0}, AA: {1}, TC: {2}, RD: {3}, RA: {4}, AD: {5}, CD: {6}, RCODE: {7}", Opcode, AuthoritativeAnswer, Truncated, RecursionDesired, RecursionAvailable, AuthenticData, CheckingDisabled, ResponseCode);
            sb.AppendLine();
            for (int i = 0; i < Questions.Count; i++)
                sb.AppendFormat("Question {0}: {1}", i + 1, Questions[i].ToString()).AppendLine();
            for (int i = 0; i < AnswerRecords.Count; i++)
                sb.AppendFormat("Answer RR {0}: {1}", i + 1, AnswerRecords[i].ToString()).AppendLine();
            for (int i = 0; i < AuthorityRecords.Count; i++)
                sb.AppendFormat("Authority RR {0}: {1}", i + 1, AuthorityRecords[i].ToString()).AppendLine();
            for (int i = 0; i < AdditionalRecords.Count; i++)
                sb.AppendFormat("Additional RR {0}: {1}", i + 1, AdditionalRecords[i].ToString()).AppendLine();
            return sb.ToString().Trim();
        }

        #endregion
    }
}
