Imports System
Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Threading.Tasks
Imports Newtonsoft.Json.Linq




Module Program
    ' Change the Main method to be non-async, and call an Async method
    Sub Main()
        ' Start the async operation
        MainAsync().GetAwaiter().GetResult()
    End Sub

    ' Async method to handle the WhatsApp operations
    Async Function MainAsync() As Task
        Dim recipientPhone As String = "15551234567" ' Replace with the recipient's phone number
        Dim messageText As String = "Here is a test message."
        Dim localFilePath As String = "C:\path\to\your\file.pdf" ' Path to your local file

        ' Call the methods with parentheses for Async execution
        Await WhatsAppAPI.SendTextMessage(recipientPhone, messageText)
        Await WhatsAppAPI.SendMediaMessage(recipientPhone, "Here is your file.", localFilePath)

        ' Wait for user input to keep the console open
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
    End Function
End Module


Module WhatsAppAPI
    Private ReadOnly accessToken As String = "YOUR_ACCESS_TOKEN" ' Replace with your access token
    Private ReadOnly phoneNumberId As String = "YOUR_PHONE_NUMBER_ID" ' Replace with your WhatsApp Business Phone Number ID

    ' Method to send a plain text message
    Public Async Function SendTextMessage(recipientPhoneNumber As String, messageText As String) As Task
        Dim url As String = $"https://graph.facebook.com/v17.0/{phoneNumberId}/messages"

        ' Create the JSON payload for text message
        Dim jsonPayload As String = "{
            ""messaging_product"": ""whatsapp"",
            ""to"": """ & recipientPhoneNumber & """,
            ""text"": {
                ""body"": """ & messageText & """
            }
        }"

        Using client As New HttpClient()
            client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)

            ' Prepare the request content
            Dim content As New StringContent(jsonPayload, Encoding.UTF8, "application/json")

            Try
                ' Send the request
                Dim response As HttpResponseMessage = Await client.PostAsync(url, content)
                Dim responseText As String = Await response.Content.ReadAsStringAsync()

                If response.IsSuccessStatusCode Then
                    Console.WriteLine("Text Message sent successfully: " & responseText)
                Else
                    Console.WriteLine("Error sending text message: " & responseText)
                End If
            Catch ex As Exception
                Console.WriteLine("Exception during text message sending: " & ex.Message)
            End Try
        End Using
    End Function

    ' Method to upload a file and get media ID
    Private Async Function UploadMediaFile(localFilePath As String) As Task(Of String)
        Dim url As String = $"https://graph.facebook.com/v17.0/{phoneNumberId}/media"

        Using client As New HttpClient()
            client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)

            ' Prepare the multipart form data
            Using form As New MultipartFormDataContent()
                Dim fileContent As New ByteArrayContent(File.ReadAllBytes(localFilePath))
                fileContent.Headers.ContentType = New MediaTypeHeaderValue("application/pdf") ' Adjust MIME type based on file
                form.Add(fileContent, "file", Path.GetFileName(localFilePath))

                Try
                    ' Send the file upload request
                    Dim response As HttpResponseMessage = Await client.PostAsync(url, form)
                    Dim responseText As String = Await response.Content.ReadAsStringAsync()

                    If response.IsSuccessStatusCode Then
                        ' Extract the media_id from the response
                        Dim jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(responseText)
                        Dim mediaId As String = jsonResponse("id").ToString()
                        Console.WriteLine("File uploaded successfully. Media ID: " & mediaId)
                        Return mediaId
                    Else
                        Console.WriteLine("Error during file upload: " & responseText)
                        Return Nothing
                    End If
                Catch ex As Exception
                    Console.WriteLine("Exception during file upload: " & ex.Message)
                    Return Nothing
                End Try
            End Using
        End Using
    End Function

    ' Method to send a message with media (document)
    Public Async Function SendMediaMessage(recipientPhoneNumber As String, messageText As String, localFilePath As String) As Task
        ' Step 1: Upload the media
        Dim mediaId As String = Await UploadMediaFile(localFilePath)

        If String.IsNullOrEmpty(mediaId) Then
            Console.WriteLine("File upload failed.")
            Return
        End If

        ' Step 2: Send the media message
        Dim url As String = $"https://graph.facebook.com/v17.0/{phoneNumberId}/messages"

        ' Create the JSON payload for media message
        Dim jsonPayload As String = "{
            ""messaging_product"": ""whatsapp"",
            ""to"": """ & recipientPhoneNumber & """,
            ""document"": {
                ""id"": """ & mediaId & """,
                ""filename"": """ & Path.GetFileName(localFilePath) & """,
                ""caption"": """ & messageText & """
            }
        }"

        Using client As New HttpClient()
            client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Bearer", accessToken)

            ' Prepare the request content
            Dim content As New StringContent(jsonPayload, Encoding.UTF8, "application/json")

            Try
                ' Send the request
                Dim response As HttpResponseMessage = Await client.PostAsync(url, content)
                Dim responseText As String = Await response.Content.ReadAsStringAsync()

                If response.IsSuccessStatusCode Then
                    Console.WriteLine("Media Message sent successfully: " & responseText)
                Else
                    Console.WriteLine("Error sending media message: " & responseText)
                End If
            Catch ex As Exception
                Console.WriteLine("Exception during media message sending: " & ex.Message)
            End Try
        End Using
    End Function
End Module
