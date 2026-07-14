using KolHaNitzachon.PhoneSystem.Application.DTOs.Recipient;
using KolHaNitzachon.PhoneSystem.Application.Interfaces.Repositories;
using KolHaNitzachon.PhoneSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace KolHaNitzachon.PhoneSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipientsController : ControllerBase
    {
        private readonly IRecipientRepository _repository;

        public RecipientsController(IRecipientRepository repository)
        {
            _repository = repository;
        }

        // GET: api/recipients
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var recipients = await _repository.GetAllAsync();

            var response = recipients.Select(x => new RecipientResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                NameRecordingUrl = x.NameRecordingUrl,
                StartDate = x.StartDate,
                EndDate = x.EndDate
            });

            return Ok(response);
        }

        // GET: api/recipients/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var recipient = await _repository.GetByIdAsync(id);

            if (recipient == null)
                return NotFound();

            var response = new RecipientResponse
            {
                Id = recipient.Id,
                Code = recipient.Code,
                Name = recipient.Name,
                NameRecordingUrl = recipient.NameRecordingUrl,
                StartDate = recipient.StartDate,
                EndDate = recipient.EndDate
            };

            return Ok(response);
        }

        // POST: api/recipients
        [HttpPost]
        public async Task<IActionResult> Create(CreateRecipientRequest request)
        {
            var recipient = new Recipient
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                NameRecordingUrl = string.Empty
            };

            await _repository.AddAsync(recipient);

            var response = new RecipientResponse
            {
                Id = recipient.Id,
                Code = recipient.Code,
                Name = recipient.Name,
                NameRecordingUrl = recipient.NameRecordingUrl,
                StartDate = recipient.StartDate,
                EndDate = recipient.EndDate
            };

            return CreatedAtAction(nameof(Get), new { id = recipient.Id }, response);
        }

        // PUT: api/recipients/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateRecipientRequest request)
        {
            //if (id != request.Id)
            //    return BadRequest();

            var recipient = await _repository.GetByIdAsync(id);

            if (recipient == null)
                return NotFound();

            recipient.Code = request.Code;
            recipient.Name = request.Name;
            recipient.NameRecordingUrl = request.NameRecordingUrl;
            recipient.StartDate = request.StartDate;
            recipient.EndDate = request.EndDate;

            await _repository.UpdateAsync(recipient);

            return NoContent();
        }

        // DELETE: api/recipients/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var recipient = await _repository.GetByIdAsync(id);

            if (recipient == null)
                return NotFound();

            await _repository.DeleteAsync(id);

            return NoContent();
        }
    }
}