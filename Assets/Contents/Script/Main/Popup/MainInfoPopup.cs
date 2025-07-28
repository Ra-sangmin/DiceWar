using UnityEngine;

public class MainInfoPopup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TermsOfConditionsPopupOn()
    {
		PopupManager.Instance.TermsOfConditionsPopupOn();
	}

	public void PrivatePolicyPopupOn()
	{
		PopupManager.Instance.PrivatePolicyPopupOn();
	}

	public void ContactUsOn()
	{
		string mailto = "wyeth123@naver.com";
		string subject = EscapeURL("");
		string body = EscapeURL("");

		Application.OpenURL("mailto:" + mailto + "?subject=" + subject + "&body=" + body);

		/*
		MailMessage mail = new MailMessage();

		mail.From = new MailAddress("sender@gmail.com");
		mail.To.Add("receiver@mtc.edu.om");
		mail.Subject = "Test Mail";
		mail.Body = "This is for testing SMTP mail from gmail";

		SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
		smtpServer.Port = 465;

		smtpServer.Credentials = new System.Net.NetworkCredential("sender@gmail.com", "senderpassword") as ICredentialsByHost;
		smtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
		smtpServer.EnableSsl = true;
		ServicePointManager.ServerCertificateValidationCallback =
		  delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		  { return true; };
		smtpServer.SendCompleted += new SendCompletedEventHandler(MailDeliveryComplete);
		smtpServer.Send(mail);
		Debug.Log("success");

		Debug.LogWarning("ContactUsOn");
		*/
	}

	string EscapeURL(string url)
	{
		return WWW.EscapeURL(url).Replace("+", "%20");
	}

	public void RateUsOn()
	{
		string url = "https://play.google.com/store/games?hl=ko";
		Application.OpenURL(url);
	}
}
