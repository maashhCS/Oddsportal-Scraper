namespace Oddsportal_Scraper.Enum;

public static class SportUrlParameter
{
    public static string GetSportUrlParameter(Sport sport)
    {
        switch (sport)
        {
            case Sport.Football:
                return "football";
            case Sport.Basketball:
                return "basketball";
            case Sport.Baseball:
                return "baseball";
            case Sport.Hockey:
                return "hockey";
            case Sport.Tennis:
                return "tennis";
            case Sport.Badminton:
                return "badminton";
            case Sport.Darts:
                return "darts";
            case Sport.Cricket:
                return "cricket";
            case Sport.MMA:
                return "mma";
            case Sport.Esports:
                return "esports";
            case Sport.Handball:
                return "handball";
            case Sport.Futsal:
                return "futsal";
            case Sport.Snooker:
                return "snooker";
            case Sport.TableTennis:
                return "table-tennis";
            case Sport.Rugby:
                return "rugby-union";
            case Sport.Volleyball:
                return "volleyball";
            case Sport.Boxing:
                return "boxing";
            case Sport.AmericanFootball:
                return "american-football";
            default:
                throw new ArgumentException("Sport not Found");
        }
    }

    public static Sport GetSportUrlParameter(string sport)
    {
        switch (sport)
        {
            case "football":
                return Sport.Football;
            case "basketball":
                return Sport.Basketball;
            case "baseball":
                return Sport.Baseball;
            case "hockey":
                return Sport.Hockey;
            case "tennis":
                return Sport.Tennis;
            case "badminton":
                return Sport.Tennis;
            case "darts":
                return Sport.Darts;
            case "cricket":
                return Sport.Cricket;
            case "mma":
                return Sport.MMA;
            case "esports":
                return Sport.Esports;
            case "handball":
                return Sport.Handball;
            case "futsal":
                return Sport.Futsal;
            case "snooker":
                return Sport.Snooker;
            case "table-tennis":
                return Sport.TableTennis;
            case "rugby-union":
                return Sport.Rugby;
            case "volleyball":
                return Sport.Volleyball;
            case "boxing":
                return Sport.Boxing;
            case "american-football":
                return Sport.AmericanFootball;
            default:
                throw new ArgumentException("Sport not Found");
        }
    }
}