﻿using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using System.ComponentModel.DataAnnotations;

namespace IntroductionWebAPI.Controllers
{
    [ApiController]
    [Route("cats")]
    public class CatController : Controller
    {
        private readonly string connString = "Host=localhost;Username=postgres;Password=12345;Database=cat_shelter_system";

        [HttpGet]
        public IActionResult GetCats(string name = "", int? age = null, string color = "",
            DateOnly? ArrivalDateAfter = null, DateOnly? ArrivalDateBefore = null)
        {
            try
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                var sql =
                    "SELECT c.\"Id\" AS CatId, c.\"Name\" AS CatName, c.\"Age\" AS CatAge, c.\"Color\" AS CatColor, c.\"ArrivalDate\" AS CatArrivalDate, " +
                    "cs.\"Id\" AS ShelterId, cs.\"Name\" AS ShelterName, cs.\"Location\" AS ShelterLocation, cs.\"CreatedAt\" AS ShelterCreatedAt " +
                    "FROM \"Cat\" c " +
                    "LEFT JOIN \"CatShelter\" cs ON cs.\"Id\" = c.\"CatShelterId\" " +
                    "WHERE 1=1 ";

                var parameters = new List<NpgsqlParameter>();
                var filterDict = new Dictionary<string, object?>
                {
                    { "name", !string.IsNullOrEmpty(name) ? name : null },
                    { "age", age.HasValue ? age : null },
                    { "color", !string.IsNullOrEmpty(color) ? color : null },
                    { "ArrivalDateAfter", ArrivalDateAfter.HasValue ? ArrivalDateAfter.Value : null },
                    { "ArrivalDateBefore", ArrivalDateBefore.HasValue ? ArrivalDateBefore.Value : null }
                };

                foreach (var (key, value) in filterDict)
                {
                    if (value != null)
                    {
                        sql += key switch
                        {
                            "name" => " AND c.\"Name\" = @name",
                            "age" => " AND c.\"Age\" = @age",
                            "color" => " AND c.\"Color\" = @color",
                            "ArrivalDateAfter" => " AND \"ArrivalDate\" > @ArrivalDateAfter",
                            "ArrivalDateBefore" => " AND \"ArrivalDate\" < @ArrivalDateBefore",
                            _ => throw new ArgumentException("Invalid filter key")
                        };

                        parameters.Add(new NpgsqlParameter($"@{key}", value));
                    }
                }

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                List<Cat> cats = [];
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Cat cat = new Cat
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("CatId")),
                        Name = reader.GetString(reader.GetOrdinal("CatName")),
                        Age = reader.GetInt32(reader.GetOrdinal("CatAge")),
                        Color = reader.GetString(reader.GetOrdinal("CatColor")),
                        ArrivalDate = reader.IsDBNull(reader.GetOrdinal("CatArrivalDate")) ?
                               null : reader.GetFieldValue<DateOnly>(reader.GetOrdinal("CatArrivalDate")),
                        CatShelterid = reader.IsDBNull(reader.GetOrdinal("ShelterId")) ? null : reader.GetGuid(reader.GetOrdinal("ShelterId"))
                    };
                    cats.Add(cat);
                }
                return Ok(cats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("{id}")]
        public IActionResult GetCat(Guid id)
        {
            try
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                var sql =
                    "SELECT c.\"Id\" AS CatId, c.\"Name\" AS CatName, c.\"Age\" AS CatAge, c.\"Color\" AS CatColor, c.\"ArrivalDate\" AS CatArrivalDate, " +
                    "cs.\"Id\" AS ShelterId, cs.\"Name\" AS ShelterName, cs.\"Location\" AS ShelterLocation, cs.\"CreatedAt\" AS ShelterCreatedAt " +
                    "FROM \"Cat\" c " +
                    "LEFT JOIN \"CatShelter\" cs ON cs.\"Id\" = c.\"CatShelterId\" " +
                    "WHERE c.\"Id\" = @id ";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);

                using var reader = cmd.ExecuteReader();
                CatShelter catShelter = new CatShelter();
                if (reader.Read())
                {
                    Cat cat = new Cat
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("CatId")),
                        Name = reader.GetString(reader.GetOrdinal("CatName")),
                        Age = reader.GetInt32(reader.GetOrdinal("CatAge")),
                        Color = reader.GetString(reader.GetOrdinal("CatColor")),
                        ArrivalDate = reader.IsDBNull(reader.GetOrdinal("CatArrivalDate")) ?
                                                   null : reader.GetFieldValue<DateOnly>(reader.GetOrdinal("CatArrivalDate")),
                        CatShelterid = reader.IsDBNull(reader.GetOrdinal("ShelterId")) ? null : reader.GetGuid(reader.GetOrdinal("ShelterId"))
                    };
                    catShelter.Cats.Add(cat);
                    return Ok(cat);
                }
                else
                {
                    return NotFound($"Cat with Id {id} not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Route("add")]
        public IActionResult PostCat([FromBody][Required] CatAddModel catAddModel)
        {
            try
            {
                using var conn = new NpgsqlConnection(connString);
                Guid catShelterId = Guid.NewGuid();
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO \"Cat\" (\"Id\", \"Name\", \"Age\", \"Color\", \"ArrivalDate\") VALUES (@id, @name, @age, @color, @arrivalDate)", conn);
                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, catShelterId);
                cmd.Parameters.AddWithValue("name", catAddModel.Name);
                cmd.Parameters.AddWithValue("age", catAddModel.Age);
                cmd.Parameters.AddWithValue("color", catAddModel.Color);
                cmd.Parameters.AddWithValue("arrivalDate", catAddModel.ArrivalDate ?? (object)DBNull.Value);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected <= 0)
                {
                    return BadRequest("Nothing was inserted.");
                }
                return StatusCode(201);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut]
        [Route("update/{id}")]
        public IActionResult PutCat(Guid id, [FromBody][Required] CatUpdateModel catUpdateModel)
        {
            if (catUpdateModel == null)
            {
                return BadRequest("Invalid update data.");
            }

            try
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                var sql = "UPDATE \"Cat\" SET ";
                var parameters = new List<NpgsqlParameter>();
                var setClauses = new List<string>();

                var updateModelProperties = typeof(CatUpdateModel).GetProperties();

                foreach (var property in updateModelProperties)
                {
                    var newValue = property.GetValue(catUpdateModel);

                    if (newValue != null)
                    {
                        var parameterName = $"@{property.Name}";
                        setClauses.Add($"\"{property.Name}\" = {parameterName}");
                        parameters.Add(new NpgsqlParameter(parameterName, newValue));
                    }
                }

                if (setClauses.Count > 0)
                {
                    sql += string.Join(", ", setClauses) + " WHERE \"Id\" = @id";
                    parameters.Add(new NpgsqlParameter("@id", id));

                    using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddRange(parameters.ToArray());

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        return NotFound($"Cat with Id {id} not found.");
                    }
                    return NoContent();
                }
                else
                {
                    return BadRequest("No fields to update.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public IActionResult DeleteCat(Guid id)
        {
            try
            {
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                using var cmd = new NpgsqlCommand("DELETE FROM \"Cat\" WHERE \"Id\" = @id", conn);

                cmd.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);

                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected <= 0)
                {
                    return NotFound($"Cat with Id {id} not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
