using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APBDTask8.Context;
using APBDTask8.Models;

namespace APBDTask8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly maksousDbContext _context;

        public ClientsController(maksousDbContext context)
        {
            _context = context;
        }

        // GET: api/Clients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClients()
        {
            if (_context.Clients == null)
            {
                return NotFound();
            }

            return await _context.Clients.ToListAsync();
        }

        // GET: api/Clients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(int id)
        {
            if (_context.Clients == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);

            if (client == null)
            {
                return NotFound();
            }

            return client;
        }

        // PUT: api/Clients/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClient(int id, Client client)
        {
            if (id != client.IdClient)
            {
                return BadRequest();
            }

            _context.Entry(client).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Clients
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Client>> PostClient(Client client)
        {
            if (_context.Clients == null)
            {
                return Problem("Entity set 'maksousDbContext.Clients'  is null.");
            }

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetClient", new { id = client.IdClient }, client);
        }

        // DELETE: api/Clients/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(int id)
        {
            if (_context.Clients == null)
            {
                return NotFound();
            }

            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ClientExists(int id)
        {
            return (_context.Clients?.Any(e => e.IdClient == id)).GetValueOrDefault();
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly maksousDbContext _context;

        public TripsController(maksousDbContext context)
        {
            _context = context;
        }

       
        // POST: api/Trips/{idTrip}/clients
        [HttpPost("{idTrip}/clients")]
        public async Task<IActionResult> AssignClientToTrip(int idTrip, [FromBody] AssignClientToTripDto clientData)
        {
            var existingClient = await _context.Clients.FirstOrDefaultAsync(c => c.Pesel == clientData.Pesel);
            if (existingClient != null)
            {
                return BadRequest("Client with the given PESEL already exists.");
            }

            var trip = await _context.Trips.FindAsync(idTrip);
            if (trip == null || trip.DateFrom < DateTime.Now)
            {
                return BadRequest("Invalid trip or trip has already occurred.");
            }

            var client = new Client
            {
                FirstName = clientData.FirstName,
                LastName = clientData.LastName,
                Email = clientData.Email,
                Telephone = clientData.Telephone,
                Pesel = clientData.Pesel
            };

            var clientTrip = new ClientTrip
            {
                IdClientNavigation = client,
                IdTripNavigation = trip,
                RegisteredAt = DateTime.Now
            };

            _context.Clients.Add(client);
            _context.ClientTrips.Add(clientTrip);

            await _context.SaveChangesAsync();
            return CreatedAtAction("GetTrips", new { id = client.IdClient }, client);
        }
        // GET: api/Trips
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripDto>>> GetTrips(int page = 1, int pageSize = 10)
        {
            var trips = await _context.Trips
                .OrderByDescending(t => t.DateFrom)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.IdCountries)
                .Include(t => t.ClientTrips)
                .ThenInclude(ct => ct.IdClientNavigation)
                .Select(t => new TripDto
                {
                    IdTrip = t.IdTrip,
                    Name = t.Name,
                    Description = t.Description,
                    DateFrom = t.DateFrom,
                    DateTo = t.DateTo,
                    MaxPeople = t.MaxPeople,
                    Countries = t.IdCountries.Select(c => new CountryDto { IdCountry = c.IdCountry, Name = c.Name }).ToList(),
                    Clients = t.ClientTrips.Select(ct => new ClientDto
                    {
                        IdClient = ct.IdClientNavigation.IdClient,
                        FirstName = ct.IdClientNavigation.FirstName,
                        LastName = ct.IdClientNavigation.LastName,
                        Email = ct.IdClientNavigation.Email,
                        Telephone = ct.IdClientNavigation.Telephone,
                        Pesel = ct.IdClientNavigation.Pesel
                    }).ToList()
                })
                .ToListAsync();

            var totalTrips = await _context.Trips.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalTrips / pageSize);

            return Ok(new
            {
                pageNum = page,
                pageSize,
                allPages = totalPages,
                trips
            });
        }

    }
    
    public class TripDto
    {
        public int IdTrip { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int MaxPeople { get; set; }
        public List<CountryDto> Countries { get; set; }
        public List<ClientDto> Clients { get; set; }
    }

    public class ClientDto
    {
        public int IdClient { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string Pesel { get; set; }
    }

    public class CountryDto
    {
        public int IdCountry { get; set; }
        public string Name { get; set; }
    }

    public class AssignClientToTripDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Telephone { get; set; }
        public string Pesel { get; set; }
        public int IdTrip { get; set; }
    }

}
