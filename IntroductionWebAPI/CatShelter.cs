﻿using System.ComponentModel.DataAnnotations;

namespace IntroductionWebAPI
{
    public class CatShelter
    {
        [Key]
        public Guid? Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(300)]
        public string? Name { get; set; }

        [Required]
        [MaxLength(400)]
        public string? Location { get; set; }

        [Required]
        public DateOnly? CreatedAt { get; set; }

        public List<Cat> Cats { get; set; } = [];
    }
}
