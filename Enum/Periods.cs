namespace Oddsportal_Scraper.Enum;

public enum PeriodType
{
    Half,
    Quarter,
    Inning,
    Set,
    Round,
    Overtime,
    Penalties,
    Period,
    Leg
}

public static class PeriodTypesExtensions
{
    public static string ToFriendlyString(this PeriodType periodType)
    {
        return periodType switch
        {
            PeriodType.Half => "Half",
            PeriodType.Quarter => "Quarter",
            PeriodType.Inning => "Inning",
            PeriodType.Set => "Set",
            PeriodType.Round => "Round",
            PeriodType.Leg => "Leg",
            PeriodType.Overtime => "Overtime",
            PeriodType.Penalties => "Penalties",
            _ => throw new ArgumentOutOfRangeException(nameof(periodType), periodType, null)
        };
    }

    public static List<PeriodType> GetPeriodTypes(Sport sport)
    {
        switch (sport)
        {
            case Sport.Football:
                return new List<PeriodType> { PeriodType.Half, PeriodType.Overtime, PeriodType.Penalties };
            case Sport.Basketball:
                return new List<PeriodType> { PeriodType.Quarter, PeriodType.Overtime };
            case Sport.Baseball:
                return new List<PeriodType> { PeriodType.Inning, PeriodType.Overtime };
            case Sport.Hockey:
                return new List<PeriodType> { PeriodType.Period, PeriodType.Overtime };
            case Sport.Tennis:
                return new List<PeriodType> { PeriodType.Set };
            case Sport.Badminton:
                return new List<PeriodType> { PeriodType.Set };
            case Sport.Darts:
                return new List<PeriodType> { PeriodType.Set };
            case Sport.Cricket:
                return new List<PeriodType> { PeriodType.Set };
            case Sport.MMA:
                return new List<PeriodType> { PeriodType.Round };
            case Sport.Esports:
                return new List<PeriodType> { PeriodType.Half, PeriodType.Overtime };
            case Sport.Handball:
                return new List<PeriodType> { PeriodType.Half, PeriodType.Overtime };
            case Sport.Futsal:
                return new List<PeriodType> { PeriodType.Half, PeriodType.Overtime };
            case Sport.Snooker:
                return new List<PeriodType> { PeriodType.Round };
            case Sport.TableTennis:
                return new List<PeriodType> { PeriodType.Set };
            case Sport.Rugby:
                return new List<PeriodType> { PeriodType.Half, PeriodType.Overtime };
            case Sport.Volleyball:
                return new List<PeriodType> { PeriodType.Set };
            case Sport.Boxing:
                return new List<PeriodType> { PeriodType.Round };
            case Sport.AmericanFootball:
                return new List<PeriodType> { PeriodType.Quarter, PeriodType.Overtime };
            default:
                throw new ArgumentException("Sport not Found");
        }
    }
}