using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace AFSD
{
    internal class TwilioInterop
    {
        string accountSID;
        string accountTOKEN;
        string accountNumber;
        string accountUSER;
        string accountPASS;
        public TwilioInterop(string sid, string token, string number, string user, string pass)
        {
            this.accountSID = sid;
            this.accountTOKEN = token;
            this.accountNumber = number;
            this.accountUSER = user;
            this.accountPASS = pass;
        }

        public void TestMessage(string target)
        {
            TwilioClient.Init(accountSID, accountTOKEN);


            MessageResource message = MessageResource.Create(
                body: "This is a test message from Twilio",
                from: new Twilio.Types.PhoneNumber(accountNumber),
                to: new Twilio.Types.PhoneNumber(target),
                pathAccountSid: this.accountSID                
            );
            //TwilioClient.Invalidate();
        }
        public void MessageWithPicture(string target, string imageURL)
        {
            TwilioClient.Init(accountSID, accountTOKEN);
            var mediaUrl = new[] {
            new Uri(imageURL)
            }.ToList();
            Console.WriteLine(imageURL);
            MessageResource message = MessageResource.Create(
                body: "You have a visiter!",
                from: new Twilio.Types.PhoneNumber(accountNumber),
                to: new Twilio.Types.PhoneNumber(target),
                pathAccountSid: this.accountSID,
                mediaUrl: mediaUrl
            );
            //TwilioClient.Invalidate();
        }

    }
}
