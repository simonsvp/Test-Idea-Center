using Idea.Models;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace IdeaCenterExamPrep
{
    [TestFixture]
    public class IdeaCenterTests
    {
        private RestClient client;

        private static string lastCreatedIdeaId;

        private const string BaseUrl = "http://144.91.123.158:82";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJkOWJlNzBhZi1kYmEzLTRkNmQtOWI5MS0yODRiMDA5MDk5YzMiLCJpYXQiOiIwNC8xNi8yMDI2IDE2OjE2OjE1IiwiVXNlcklkIjoiM2U4YTMxNTQtMjg5MC00N2FlLTUzOWEtMDhkZTc2YTJkM2VjIiwiRW1haWwiOiJzaW1vbnNAYWJ2LmJnIiwiVXNlck5hbWUiOiJzaW1vbnN2cCIsImV4cCI6MTc3NjM3Nzc3NSwiaXNzIjoiSWRlYUNlbnRlcl9BcHBfU29mdFVuaSIsImF1ZCI6IklkZWFDZW50ZXJfV2ViQVBJX1NvZnRVbmkifQ.x9MfrxPVBxWHhgS-YKm5zFdjERbaHX7VK_GiDDZONQ0";

        private const string LoginEmail = "simons@abv.bg";
        private const string LoginPassword = "123123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new
            {
                email,
                password
            });

            var response = tempClient.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(
                    $"Failed to authenticate. Status code: {response.StatusCode}. Response: {response.Content}");
            }

            var content = JsonSerializer.Deserialize<JsonElement>(response.Content!);

            var token = content.GetProperty("accessToken").GetString();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Access token not found in the response.");
            }

            return token;
        }

        [Test, Order(1)]
        public void Test_CreateIdeaWithRequiredFields()
        {
            var request = new RestRequest("/api/Idea/Create", Method.Post);

            var newIdea = new IdeaDTO
            {
                Title = "My first API idea",
                Description = "This is created by automated test.",
                Url = ""
            };

            request.AddJsonBody(newIdea);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseDto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);

            Assert.That(responseDto, Is.Not.Null);
            Assert.That(responseDto!.Msg, Is.EqualTo("Successfully created!"));

            if (!string.IsNullOrWhiteSpace(responseDto.IdeaId))
            {
                lastCreatedIdeaId = responseDto.IdeaId;
            }
        }

        [Test, Order(2)]
        public void Test_GetAllIdeas()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var ideas = JsonSerializer.Deserialize<List<IdeaDTO>>(response.Content!);

            Assert.That(ideas, Is.Not.Null);
            Assert.That(ideas!.Count, Is.GreaterThan(0));

            lastCreatedIdeaId = ideas.Last().Id!;

            Assert.That(lastCreatedIdeaId, Is.Not.Null.And.Not.Empty);
        }

        [Test, Order(3)]
        public void Test_EditLastCreatedIdea()
        {
            var request = new RestRequest("/api/Idea/Edit", Method.Put);

            request.AddQueryParameter("ideaId", lastCreatedIdeaId);

            var editedIdea = new IdeaDTO
            {
                Title = "Edited API Idea",
                Description = "This idea was edited by automated test.",
                Url = ""
            };

            request.AddJsonBody(editedIdea);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseDto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);

            Assert.That(responseDto, Is.Not.Null);
            Assert.That(responseDto!.Msg, Is.EqualTo("Edited successfully"));
        }

        [Test, Order(4)]
        public void Test_DeleteLastEditedIdea()
        {
            Assert.That(lastCreatedIdeaId, Is.Not.Null.And.Not.Empty);

            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }

}