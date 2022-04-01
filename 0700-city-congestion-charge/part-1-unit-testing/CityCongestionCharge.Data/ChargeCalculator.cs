using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CityCongestionCharge.Data;

public class ChargeCalculator
{
    /// <summary>
    /// Represents a single trip
    /// </summary>
    /// <param name="Entering">Timestamp when the car entered the city</param>
    /// <param name="Leaving">Timestamp when the car left the city</param>
    /// <param name="DetectionsInside">Timestamps between <paramref name="Entering"/> 
    ///     and <paramref name="Leaving"/> when the car has been detected driving inside the city
    /// </param>
    /// <remarks>
    /// Note that <paramref name="Leaving"/> can be <c>null</c>. In that case, the trip is not over
    /// (i.e. the car is still inside the city).
    /// </remarks>
    public record Trip(DateTime Entering, DateTime? Leaving, IEnumerable<DateTime> DetectionsInside);

    /// <summary>
    /// Parameters for fee calculation
    /// </summary>
    /// <param name="Trips">Data of the trips relevant for the day to calculate</param>
    /// <param name="CarType">Car type for which the fee has to be calculated</param>
    /// <param name="DayToCalculate">Day for which the fee has to be calculated</param>
    /// <param name="IsElectricOrHybrid">Indicates whether the car was an EV or HEV</param>
    public record CalculationParameters(IEnumerable<Trip> Trips, DateTime DayToCalculate, CarType CarType, bool IsElectricOrHybrid);

    /// <summary>
    /// Calculate the charge for a trip.
    /// </summary>
    /// <param name="parameters">Calculation parameters</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if fee cannot be calculated. This is the case of <paramref name="dayToCalculate"/>
    /// is today and the trip has not been completed (i.e. car has not left the city yet).
    /// </exception>
    /// <returns>Total fee for the given day</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "For dependency injection")]
    public decimal CalculateFee(CalculationParameters parameters)
    {

        bool rushHourSet = false;
        decimal congestionFee = 0;
        int timeInCity = 0;
        decimal max = 0m;

        IEnumerable<Trip> relevantTrips = filterRelevantTrips(parameters.Trips, parameters.DayToCalculate);
        relevantTrips.OrderBy(relevantTrips => relevantTrips.Entering);

        foreach (Trip trip in relevantTrips)
        {
            if (!parameters.IsElectricOrHybrid) {
                if (trip.Leaving == null)
                {
                    timeInCity += (TimeOnly.MaxValue.Hour) - TimeOnly.MinValue.Add(trip.Entering.TimeOfDay).Hour;
                }
                else
                {
                    int overtime = 1;
                    var leaving = trip.Leaving.Value.Hour;
                    if (trip.Leaving.Value.Hour == 0)
                    {
                        overtime = 0;
                        leaving = 24;
                    }
                    //var leaving = trip.Leaving.Value.Hour == 0 ? 24 : trip.Leaving.Value.Hour;
                    //int overtime = trip.Leaving.Value.Minute == 0 ? 0 : 1;
                    timeInCity += (leaving + overtime) - trip.Entering.Hour;
                }
            }
            if (!rushHourSet)
            {
                if (isInRushHour(trip.Entering) || isInRushHour(trip.Entering) || trip.DetectionsInside.Any(d => isInRushHour(d)))
                {
                    congestionFee += 3;
                    rushHourSet = true;
                }
            }
        }
        congestionFee += timeInCity;
        switch (parameters.CarType)
        {
            case CarType.PassengerCar:
                max = 20m;
                break;
            case CarType.Motorcycle:
                congestionFee = congestionFee * 0.5m;
                max = 10m;
                break;
            case CarType.Lorry:
                congestionFee = congestionFee * 1.5m;
                max = 30m;
                break;
            case CarType.Van:
                congestionFee = congestionFee * 1.5m;
                max = 30m;
                break;
            default:
                max = 20m;
                break;
        }
        return congestionFee > max ? max : congestionFee;
    }

    private IEnumerable<Trip> filterRelevantTrips(IEnumerable<Trip> trips, DateTime dayToCalculate)
    {
        List<Trip> filteredTrips = new List<Trip>();
        DateTime entering = DateTime.Today;
        DateTime leaving = DateTime.Today;
        foreach (Trip trip in trips)
        {
            
            if (trip.Entering.Day == dayToCalculate.Day)
            {
                entering = trip.Entering;
                if (!trip.Leaving.HasValue || trip.Leaving.Value.Day > dayToCalculate.Day)
                {
                    leaving = dayToCalculate.AddDays(1);
                }
                else
                {
                    leaving = trip.Leaving.Value;
                }
            }
            else if(trip.Entering < dayToCalculate)
            {
                if (!trip.Leaving.HasValue)
                {
                    entering = dayToCalculate.AddDays(-1);
                    leaving = DateTime.Today;
                }
                if (trip.Leaving.HasValue && trip.Leaving.Value.Day == dayToCalculate.Day)
                {
                    entering = dayToCalculate.AddDays(-1);
                    leaving = trip.Leaving.Value;
                }
            }
            filteredTrips.Add(new Trip(entering,leaving,trip.DetectionsInside));
        }
        return filteredTrips;
    }

    private bool isInRushHour(DateTime dateTime)
    {
        if (dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday) return false;
        TimeOnly morningRushHourStart = new TimeOnly(7, 30);
        TimeOnly morningRushHourEnd = new TimeOnly(10,0);
        TimeOnly eveningRushHourStart = new TimeOnly(15, 30);
        TimeOnly eveningRushHourEnd = new TimeOnly(18,0);
        if (dateTime.Hour >= morningRushHourStart.Hour && dateTime.Hour <= morningRushHourEnd.Hour) return true;
        if (dateTime.Hour >= eveningRushHourStart.Hour && dateTime.Hour <= eveningRushHourEnd.Hour) return true;
        return false;
    }
}
