﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DNS.Protocol {
    public interface IRequest : IMessage {
        int Id { get; set; }
        OperationCode OperationCode { get; set; }
        bool RecursionDesired { get; set; }
        //int QuestionCount { get; }
        //void AddQuestion(Question question);
    }

    public class Request : IRequest {
        private static readonly Random RANDOM = new Random();

        private IList<Question> questions;
        private Header header;

        public static Request FromArray(byte[] message) {
            Header header = Header.FromArray(message);

            if (header.Response || header.QuestionCount == 0 ||
                    header.AdditionalRecordCount + header.AnswerRecordCount + header.AuthorityRecordCount > 0 || 
                    header.ResponseCode != ResponseCode.NoError) {

                throw new ArgumentException("Invalid request message");
            }

            return new Request(header, Question.GetAllFromArray(message, header.Size, header.QuestionCount));
        }

        public Request(Header header, IList<Question> questions) {
            this.header = header;
            this.questions = questions;
        }

        public Request() {
            this.questions = new List<Question>();
            this.header = new Header();

            this.header.OperationCode = OperationCode.Query;
            this.header.Response = false;
            this.header.Id = RANDOM.Next(UInt16.MaxValue);
        }

        public Request(IRequest request) {
            this.header = new Header();
            this.questions = new List<Question>(request.Questions);

            this.header.Response = false;

            Id = request.Id;
            OperationCode = request.OperationCode;
            RecursionDesired = request.RecursionDesired;
        }

        public IList<Question> Questions {
            get { return questions; }
        }

        public int Size {
            get { return header.Size + questions.Sum(q => q.Size); }
        }

        public int Id {
            get { return header.Id; }
            set { header.Id = value; }
        }

        public OperationCode OperationCode {
            get { return header.OperationCode; }
            set { header.OperationCode = value; }
        }

        public bool RecursionDesired {
            get { return header.RecursionDesired; }
            set { header.RecursionDesired = value; }
        }

        /*public int QuestionCount {
            get { return header.QuestionCount; }
        }*/

        /*public void AddQuestion(Question question) {
            questions.Add(question);
            header.QuestionCount = questions.Count;
        }*/

        public byte[] ToArray(bool lengthPrefix = false) {
            UpdateHeader();
            Marshalling.ByteStream result = new Marshalling.ByteStream(Size + (lengthPrefix ? 2 : 0));

            if (lengthPrefix) {
                byte[] length = BitConverter.GetBytes((ushort) Size);

                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(length);
                }

                result.Append(length);
            }

            result
                .Append(header.ToArray())
                .Append(questions.Select(q => q.ToArray()));

            return result.ToArray();
        }

        public override string ToString() {
            UpdateHeader();

            return Marshalling.Object.New(this)
                .Add("Header", header)
                .Add("Questions")
                .ToString();
        }

        private void UpdateHeader() {
            header.QuestionCount = questions.Count;
        }
    }
}
