using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace BikeTogether.Api.Controllers;

/// <summary>
/// Web API for managing cars
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public partial class CarsController : ControllerBase
{
    private readonly CccDataContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CarsController"/> class.
    /// </summary>
    /// <param name="context">Entity Framework data context</param>
    public CarsController(CccDataContext context)
    {
        this.context = context;
    }

    #region Data transfer objects
    /// <summary>
    /// DTO used for adding new cars
    /// </summary>
    /// <remarks>
    /// Does not contain ID as the ID is generated by the server.
    /// </remarks>
    public class AddCarDto
    {
        [MaxLength(10)]
        public string LicensePlate { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Make { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Model { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        public CarType CarType { get; set; }

        public bool IsElectricOrHybrid { get; set; }

        [MaxLength(50)]
        public string OwnerFirstName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string OwnerLastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string OwnerAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO describing a car
    /// </summary>
    public class CarResultDto : AddCarDto
    {
        /// <summary>
        /// ID of the car (primary key)
        /// </summary>
        [Required]
        public int Id { get; set; }
    }

    /// <summary>
    /// Converts a given <see cref="Car"/> into a <see cref="CarResultDto"/>
    /// </summary>
    private static CarResultDto CarToResultDto(Car c)
        => new()
        {
            Id = c.Id,
            LicensePlate = c.LicensePlate,
            Make = c.Make,
            Model = c.Model,
            Color = c.Color,
            CarType = c.CarType,
            IsElectricOrHybrid = c.IsElectricOrHybrid,
            OwnerFirstName = c.Owner!.FirstName,
            OwnerLastName = c.Owner!.LastName,
            OwnerAddress = c.Owner!.Address,
        };

    /// <summary>
    /// DTO for patching a car. Specify values only for those properties that should be updated.
    /// </summary>
    public class PatchCarDto
    {
        [MaxLength(10)]
        public string? LicensePlate { get; set; }

        [MaxLength(50)]
        public string? Make { get; set; }

        [MaxLength(50)]
        public string? Model { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        public CarType? CarType { get; set; }

        public bool? IsElectricOrHybrid { get; set; }
    }
    #endregion

    /// <summary>
    /// Handles specific SQL Server-related exception
    /// </summary>
    /// <param name="ex">Exception that happend when accessing SQL Server</param>
    /// <returns>
    /// If the exception leads to a specific HTTP response, the corresponding
    /// <see cref="ActionResult"/> is returned. If there is not specific handling
    /// of the exception, <c>null</c> is returned.
    /// </returns>
    private ActionResult? TryHandleDatabaseError(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlException)
        {
            if (sqlException.Number == 2601) // Cannot insert duplicate key row in object with unique index.
            {
                return BadRequest(new ProblemDetails
                {
                    Type = "https://www.htl-leonding.at/api/errors/duplicate-key",
                    Title = "Duplicate key",
                    Status = (int)HttpStatusCode.Conflict,
                    Detail = sqlException.Message
                });
            }
        }

        return null;
    }

    /// <summary>
    /// Get a list of cars
    /// </summary>
    /// <param name="licensePlateFilter">Optional license plate filter</param>
    /// <returns>
    /// List of all cars optionally filtered by <paramref name="licensePlateFilter"/>.
    /// If a <paramref name="licensePlateFilter"/> was specified, only those cars are returned whose
    /// <see cref="Car.LicensePlate"/> contain the <paramref name="licensePlateFilter"/>.
    /// </returns>
    [HttpGet(Name = nameof(GetCars))]
    public async Task<ActionResult<IEnumerable<CarResultDto>>> GetCars([FromQuery(Name = "lpFilter")] string? licensePlateFilter)
    {
        string filter = licensePlateFilter ?? "";

        var cars = await context.Cars.Include(c => c.Owner).Where(c => c.LicensePlate.Contains(filter)).Select(c => CarToResultDto(c)).ToListAsync();
        return Ok(cars);
    }

    /// <summary>
    /// Adds a car
    /// </summary>
    /// <param name="c">Car to add</param>
    /// <returns>Data about the created car (including primary key)</returns>
    /// <response code="201">Car created</response>
    /// <response code="409">Duplicate key error (e.g. license plate already taken)</response>
    [HttpPost(Name = nameof(AddCar))]
    [ProducesResponseType(typeof(CarResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CarResultDto>> AddCar(AddCarDto c)
    {
        if (context.Cars.FirstOrDefaultAsync(v => v.LicensePlate == c.LicensePlate) == null) return StatusCode(409);
        try
        {
            Car car = new Car();
            car.IsElectricOrHybrid = c.IsElectricOrHybrid;
            car.LicensePlate = c.LicensePlate;
            car.Make = c.Make;
            car.Model = c.Model;
            car.Color = c.Color;
            car.CarType = car.CarType;
            car.Owner = new Owner() { FirstName = c.OwnerFirstName, Address = c.OwnerAddress, LastName = c.OwnerLastName };
            context.Add(car);
            await context.SaveChangesAsync();
            return Created(nameof(GetCarById), (car.Id, CarToResultDto(car)));
        }
        catch (DbUpdateException ex)
        {
            var result = TryHandleDatabaseError(ex);
            if (result != null) return result;
            throw;
        }

    }

    /// <summary>
    /// Patches a car
    /// </summary>
    /// <param name="id">Id of the car to patch</param>
    /// <param name="c">Data to patch</param>
    /// <returns>Data about the patched car</returns>
    /// <response code="404">Car not found</response>
    [HttpPatch("{id}", Name = nameof(PatchCar))]
    [ProducesResponseType(typeof(CarResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CarResultDto>> PatchCar(int id, PatchCarDto c)
    {
        var car = await context.Cars.FirstOrDefaultAsync(c => c.Id == id);
        try
        {
            if (car != null)
            {
                car.IsElectricOrHybrid = c.IsElectricOrHybrid.HasValue ? c.IsElectricOrHybrid.Value : car.IsElectricOrHybrid;
                car.LicensePlate = c.LicensePlate != null ? c.LicensePlate : car.LicensePlate;
                car.Make = c.Make != null ? c.Make : car.Make;
                car.Model = c.Model != null ? c.Model : car.Model;
                car.Color = c.Color != null ? c.Color : car.Color;
                car.CarType = c.CarType.HasValue ? c.CarType.Value : car.CarType;

                await context.SaveChangesAsync();
                return Ok(CarToResultDto(car));
            }
            else
            {
                return NotFound();
            }
        }
        catch (DbUpdateException ex)
        {
            var result = TryHandleDatabaseError(ex);
            if (result != null) return result;
            throw;
        }
    }

    /// <summary>
    /// Get a single car by ID
    /// </summary>
    /// <param name="id">Id of the car to get</param>
    /// <response code="200">Car found</response>
    /// <response code="404">Car not found</response>
    [HttpGet("{id}", Name = nameof(GetCarById))]
    [ProducesResponseType(typeof(CarResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CarResultDto>> GetCarById(int id)
    {
        var car = await context.Cars.FirstOrDefaultAsync(c => c.Id == id); 

        if (car != null)
        {
            return Ok(CarToResultDto(car));
        }
        else
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Delete a single car by ID
    /// </summary>
    /// <param name="id">Id of the car to delete</param>
    /// <response code="204">Car deleted</response>
    /// <response code="404">Car not found</response>
    [HttpDelete("{id}", Name = nameof(DeleteCarById))]
    public async Task<ActionResult> DeleteCarById(int id)
    {
        var car = await context.Cars.FirstOrDefaultAsync(c => c.Id == id); // var because Car is not nullable but query can return null
        
        if (car != null)
        {
            context.Cars.Remove(car);
            await context.SaveChangesAsync();
            return NoContent();
        }
        else
        {
            return NotFound();
        }
        
    }
}
