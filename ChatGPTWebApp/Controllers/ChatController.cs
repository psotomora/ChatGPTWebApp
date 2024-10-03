using Microsoft.AspNetCore.Mvc;
using OpenAI;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using OpenAI.Chat;
using OpenAI.Models;
using System.Reflection;


namespace ChatGPTWebApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly IOpenAIService _openAIService;
        private static List<ChatMessage> _chatMessages = new List<ChatMessage>();

        public ChatController(IConfiguration configuration)
        {
            _openAIService = new OpenAIService(new OpenAiOptions
            {
                ApiKey = configuration["OpenAI:ApiKey"]
            });
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormFile uploadedFile, string userQuestion)
        {
            if (uploadedFile != null)
            {
                // Leer el contenido del archivo subido
                string documentText = "";
                using (var reader = new StreamReader(uploadedFile.OpenReadStream()))
                {
                    documentText = await reader.ReadToEndAsync();
                }

                // Inicializar la conversación con el documento como contexto
                _chatMessages.Clear(); // Limpiar cualquier mensaje previo

                // Crear el mensaje del sistema
                var systemMessage = new ChatMessage
                {
                    Role = "system",
                    Content = "Utiliza el siguiente documento como contexto para responder las preguntas del usuario."
                };
                _chatMessages.Add(systemMessage);

                // Agregar el documento como mensaje del usuario
                var userDocumentMessage = new ChatMessage
                {
                    Role = "user",
                    Content = documentText
                };
                _chatMessages.Add(userDocumentMessage);

                ViewBag.Message = "Documento cargado exitosamente. Ahora puedes hacer preguntas.";
            }
            else if (!string.IsNullOrEmpty(userQuestion))
            {
                if (_chatMessages == null || _chatMessages.Count == 0)
                {
                    ViewBag.Message = "Por favor, carga un documento antes de hacer preguntas.";
                }
                else
                {
                    // Agregar la pregunta del usuario al historial de mensajes
                    var userQuestionMessage = new ChatMessage
                    {
                        Role = "user",
                        Content = userQuestion
                    };
                    _chatMessages.Add(userQuestionMessage);

                    // Crear la solicitud de chat
                    var chatCompletionCreateRequest = new ChatCompletionCreateRequest
                    {
                        Messages = _chatMessages,
                        Model = Models.Gpt_4, // Especificar GPT-4 como modelo
                        MaxTokens = 2048 // Puedes ajustar este valor según tus necesidades y límites de la API
                    };

                    // Obtener la respuesta del asistente
                    var response = await _openAIService.ChatCompletion.CreateCompletion(chatCompletionCreateRequest);

                    if (response.Successful)
                    {
                        var reply = response.Choices[0].Message.Content;

                        // Agregar la respuesta del asistente al historial de mensajes
                        var assistantMessage = new ChatMessage
                        {
                            Role = "assistant",
                            Content = reply
                        };
                        _chatMessages.Add(assistantMessage);

                        ViewBag.Response = reply;
                    }
                    else
                    {
                        ViewBag.Message = "Ocurrió un error al obtener la respuesta de ChatGPT.";
                        if (response.Error != null)
                        {
                            ViewBag.Message += $" Error: {response.Error.Message}";
                        }
                    }
                }
            }

            return View();
        }
    }
}
