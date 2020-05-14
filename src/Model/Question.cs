﻿using System.Text.Json.Serialization;

namespace BlazorQuiz.Model
{
    public partial class Question
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("imageData")]
        public string ImageContents { get; set; }

        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; }
    }
}