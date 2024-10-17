using System;
using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Encodings.Web;

namespace WebServer.Utils.Requests
{
    public class APIRequest
    {
        private WebRequest request;
        private Stream dataStream;
        private string status;

        public String Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
            }
        }

        public APIRequest(string url)
        {
            // Create a request using a URL that can receive a post.

            request = WebRequest.Create(url);
        }

        public APIRequest(string url, string method)
            : this(url)
        {

            if (method.Equals("GET") || method.Equals("POST"))
            {
                // Set the Method property of the request to POST.
                request.Method = method;
            }
            else
            {
                throw new Exception("Invalid Method Type");
            }
        }

        public APIRequest(string url, string method, string data, string header=""): this(url, method)
        {

            if (request.Method == "POST")
            {
                // Create POST data and convert it to a byte array.
                byte[] byteArray = Encoding.UTF8.GetBytes(data);

                // Set the ContentType property of the WebRequest.
                // request.ContentType = "application/x-www-form-urlencoded";
                request.ContentType = "application/json";
                request.Headers.Add (header);

                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;
                // Get the request stream.
                dataStream = request.GetRequestStream();

                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);

                // Close the Stream object.
                dataStream.Close();



                // String finalUrl = string.Format("{0}{1}", url, "?" + data);
          

                WebResponse response = request.GetResponse();

                //Now, we read the response (the string), and output it.
                dataStream = response.GetResponseStream();
                Status = new StreamReader(response.GetResponseStream()).ReadToEnd();

            }
            else
            {
                //String finalUrl = string.Format("{0}{1}", url, "?" +data);
                //request = WebRequest.Create(finalUrl);

                WebResponse response = request.GetResponse();

                //Now, we read the response (the string), and output it.
                dataStream = response.GetResponseStream();
                Status = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }

        }




        //public APIRequest(string url, string method, string data)
        //    : this(url, method)
        //{

        //    // Create POST data and convert it to a byte array.
        //    string postData = data;
        //    byte[] byteArray = Encoding.UTF8.GetBytes(postData);

        //    // Set the ContentType property of the WebRequest.
        //    request.ContentType = "application/x-www-form-urlencoded";

        //    // Set the ContentLength property of the WebRequest.
        //    request.ContentLength = byteArray.Length;

        //    // Get the request stream.
        //    dataStream = request.GetRequestStream();

        //    // Write the data to the request stream.
        //    dataStream.Write(byteArray, 0, byteArray.Length);

        //    // Close the Stream object.
        //    dataStream.Close();

        //}

        public string GetResponse()
        {
            // Get the original response.
            WebResponse response = request.GetResponse();

            this.Status = ((HttpWebResponse)response).StatusDescription;

            // Get the stream containing all content returned by the requested server.
            dataStream = response.GetResponseStream();

            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);

            // Read the content fully up to the end.
            string responseFromServer = reader.ReadToEnd();

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

    }
}

