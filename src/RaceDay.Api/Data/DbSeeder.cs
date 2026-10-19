using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Models.Enums;

namespace RaceDay.Api.Data;

/// <summary>
/// Seeds the same rows as docs/raceday-schema.sql (same names, emails,
/// events, categories, routes, weather, enrolments and results), so the
/// Development database and the Part 1 SQL script describe the same world.
/// Only runs when Users is empty, so it never duplicates data on restart.
/// The demo password for every seeded user is "RaceDay@2026".
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "RaceDay@2026";

    public static async Task SeedAsync(RaceDayDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var hasher = new PasswordHasher<User>();

        var thandiwe = new User { FullName = "Thandiwe Mokoena", Email = "thandiwe.mokoena@raceday.co.za", Role = Roles.Organiser, PhoneNumber = "0821234567" };
        thandiwe.PasswordHash = hasher.HashPassword(thandiwe, DemoPassword);

        var johan = new User { FullName = "Johan van der Merwe", Email = "johan.vdm@raceday.co.za", Role = Roles.Organiser, PhoneNumber = "0837654321" };
        johan.PasswordHash = hasher.HashPassword(johan, DemoPassword);

        var lindiwe = new User { FullName = "Lindiwe Dlamini", Email = "lindiwe.dlamini@example.com", Role = Roles.Participant, PhoneNumber = "0731112222" };
        lindiwe.PasswordHash = hasher.HashPassword(lindiwe, DemoPassword);

        var ryan = new User { FullName = "Ryan Naidoo", Email = "ryan.naidoo@example.com", Role = Roles.Participant, PhoneNumber = "0723334444" };
        ryan.PasswordHash = hasher.HashPassword(ryan, DemoPassword);

        context.Users.AddRange(thandiwe, johan, lindiwe, ryan);

        var comrades = new Event
        {
            Organiser = thandiwe,
            Name = "Pietermaritzburg to Durban Classic",
            Description = "Ultramarathon following the traditional Comrades-style route.",
            DisciplineType = DisciplineTypes.Running,
            EventDate = new DateOnly(2026, 6, 14),
            StartTime = new TimeOnly(5, 30),
            Province = "KwaZulu-Natal",
            Venue = "Pietermaritzburg City Hall",
            Status = EventStatuses.Completed,
        };
        var capeTour = new Event
        {
            Organiser = thandiwe,
            Name = "Cape Peninsula Cycle Tour",
            Description = "Scenic road cycling event around the Cape Peninsula.",
            DisciplineType = DisciplineTypes.Cycling,
            EventDate = new DateOnly(2026, 3, 8),
            StartTime = new TimeOnly(6, 0),
            Province = "Western Cape",
            Venue = "Grand Parade, Cape Town",
            Status = EventStatuses.Completed,
        };
        var sowetoWalk = new Event
        {
            Organiser = johan,
            Name = "Soweto Community Fun Walk",
            Description = "Family-friendly charity walk through Soweto.",
            DisciplineType = DisciplineTypes.Walking,
            EventDate = new DateOnly(2026, 4, 19),
            StartTime = new TimeOnly(7, 0),
            Province = "Gauteng",
            Venue = "Orlando Stadium",
            Status = EventStatuses.Completed,
        };
        var joburgNightRun = new Event
        {
            Organiser = johan,
            Name = "Joburg Night Run",
            Description = "Floodlit night run and fun run around Zoo Lake.",
            DisciplineType = DisciplineTypes.Running,
            EventDate = new DateOnly(2027, 3, 13),
            StartTime = new TimeOnly(19, 0),
            Province = "Gauteng",
            Venue = "Zoo Lake",
            Status = EventStatuses.Published,
        };

        context.Events.AddRange(comrades, capeTour, sowetoWalk, joburgNightRun);

        var ultra87 = new Category { Event = comrades, Name = "87km Ultramarathon", DistanceKm = 87.00m, EntryFee = 850.00m, MaxParticipants = 5000, AgeGroup = "Open" };
        var half21 = new Category { Event = comrades, Name = "21km Half Marathon", DistanceKm = 21.10m, EntryFee = 450.00m, MaxParticipants = 2000, AgeGroup = "Open" };
        var cycle109 = new Category { Event = capeTour, Name = "109km Cycle Race", DistanceKm = 109.00m, EntryFee = 650.00m, MaxParticipants = 3000, AgeGroup = "Open" };
        var funRide45 = new Category { Event = capeTour, Name = "45km Fun Ride", DistanceKm = 45.00m, EntryFee = 350.00m, MaxParticipants = 1500, AgeGroup = "Open" };
        var funWalk5 = new Category { Event = sowetoWalk, Name = "5km Fun Walk", DistanceKm = 5.00m, EntryFee = 0.00m, MaxParticipants = 1000, AgeGroup = "Open" };
        var charityWalk10 = new Category { Event = sowetoWalk, Name = "10km Charity Walk", DistanceKm = 10.00m, EntryFee = 50.00m, MaxParticipants = 800, AgeGroup = "Open" };
        var nightRun10 = new Category { Event = joburgNightRun, Name = "10km Run", DistanceKm = 10.00m, EntryFee = 150.00m, MaxParticipants = 1000, AgeGroup = "Open" };
        var nightFunRun5 = new Category { Event = joburgNightRun, Name = "5km Fun Run", DistanceKm = 5.00m, EntryFee = 80.00m, MaxParticipants = 1500, AgeGroup = "Under 20" };

        context.Categories.AddRange(ultra87, half21, cycle109, funRide45, funWalk5, charityWalk10, nightRun10, nightFunRun5);

        context.Routes.AddRange(
            new CourseRoute { Category = ultra87, StartPoint = "Pietermaritzburg City Hall", EndPoint = "Kingsmead Stadium, Durban", ElevationGainM = 1200, MapUrl = "https://maps.raceday.co.za/routes/pmb-durban-87k" },
            new CourseRoute { Category = half21, StartPoint = "Pietermaritzburg City Hall", EndPoint = "Cascades, Pietermaritzburg", ElevationGainM = 350, MapUrl = "https://maps.raceday.co.za/routes/pmb-half" },
            new CourseRoute { Category = cycle109, StartPoint = "Grand Parade, Cape Town", EndPoint = "Grand Parade, Cape Town", ElevationGainM = 980, MapUrl = "https://maps.raceday.co.za/routes/cape-peninsula-109k" },
            new CourseRoute { Category = funWalk5, StartPoint = "Orlando Stadium", EndPoint = "Orlando Stadium", ElevationGainM = 20, MapUrl = "https://maps.raceday.co.za/routes/soweto-5k" }
        );

        context.WeatherSnapshots.AddRange(
            new WeatherSnapshot { Event = comrades, TemperatureC = 14.5m, WindSpeedKmh = 12.0m, ConditionDescription = "Clear skies, cool start", Source = "OpenWeatherMap" },
            new WeatherSnapshot { Event = capeTour, TemperatureC = 19.0m, WindSpeedKmh = 22.5m, ConditionDescription = "Partly cloudy, moderate coastal wind", Source = "OpenWeatherMap" },
            new WeatherSnapshot { Event = sowetoWalk, TemperatureC = 16.0m, WindSpeedKmh = 8.0m, ConditionDescription = "Overcast, light drizzle expected", Source = "OpenWeatherMap" }
        );

        var lindiweUltra = new Enrolment { Participant = lindiwe, Category = ultra87, BibNumber = "A1001", Status = EnrolmentStatuses.Confirmed };
        var lindiweFunWalk = new Enrolment { Participant = lindiwe, Category = funWalk5, BibNumber = "C5001", Status = EnrolmentStatuses.Confirmed };
        var ryanHalf = new Enrolment { Participant = ryan, Category = half21, BibNumber = "B2001", Status = EnrolmentStatuses.Confirmed };
        var ryanCycle = new Enrolment { Participant = ryan, Category = cycle109, BibNumber = "D3001", Status = EnrolmentStatuses.Confirmed };

        context.Enrolments.AddRange(lindiweUltra, lindiweFunWalk, ryanHalf, ryanCycle);

        context.Results.AddRange(
            new Result { Enrolment = lindiweUltra, CapturedBy = thandiwe, FinishTime = new TimeOnly(8, 42, 15), Position = 134, Status = ResultStatuses.Finished },
            new Result { Enrolment = ryanHalf, CapturedBy = thandiwe, FinishTime = new TimeOnly(1, 58, 3), Position = 27, Status = ResultStatuses.Finished }
        );

        await context.SaveChangesAsync();
    }
}
