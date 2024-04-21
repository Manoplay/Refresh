using Refresh.GameServer.Authentication;
using Refresh.GameServer.Database;
using Refresh.GameServer.Endpoints.Game.Levels.FilterSettings;
using Refresh.GameServer.Types.Contests;
using Refresh.GameServer.Types.Levels;
using Refresh.GameServer.Types.UserData;

namespace RefreshTests.GameServer.Tests.Levels;

public class ContestTests : GameServerTest
{
    private const string Banner = "https://i.imgur.com/n80NswV.png";
    
    private GameContest CreateContest(TestContext context, GameUser? organizer = null)
    {
        organizer ??= context.CreateUser();
        GameContest contest = new()
        {
            ContestId = "ut",
            Organizer = organizer,
            BannerUrl = Banner,
            ContestTitle = "unit test",
            ContestSummary = "summary",
            ContestTag = "#ut",
            ContestRulesMarkdown = "# unit test",
            CreationDate = context.Time.Now,
            StartDate = context.Time.Now,
            EndDate = context.Time.Now + TimeSpan.FromMilliseconds(10),
        };
        
        context.Database.CreateContest(contest);

        return contest;
    }
    
    [Test]
    public void CanCreateContest()
    {
        using TestContext context = this.GetServer(false);
        GameUser organizer = context.CreateUser();
        Assert.That(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            context.Database.CreateContest(new GameContest
            {
                ContestId = "contest",
                Organizer = organizer,
                BannerUrl = Banner,
                ContestTitle = "The Contest Contest",
                ContestSummary = "a contest about contests",
                ContestTag = "#cc1",
                ContestRulesMarkdown = "# test",
                CreationDate = DateTimeOffset.FromUnixTimeMilliseconds(0),
                StartDate = DateTimeOffset.FromUnixTimeMilliseconds(1),
                EndDate = DateTimeOffset.FromUnixTimeMilliseconds(2),
            });
        }, Throws.Nothing);

        GameContest? contest = context.Database.GetContestById("contest");
        Assert.That(contest, Is.Not.Null);
        Assert.That(contest.ContestTitle, Is.EqualTo("The Contest Contest"));
        Assert.That(contest.Organizer, Is.EqualTo(organizer));
    }

    [Test]
    public void LevelsShowUpInCategory()
    {
        using TestContext context = this.GetServer(false);
        GameUser user = context.CreateUser();
        
        context.Time.TimestampMilliseconds = 500;
        GameContest contest = this.CreateContest(context);

        context.CreateLevel(user, "#ut level 1");
        context.CreateLevel(user, "level #ut 2");
        context.CreateLevel(user, "#ut level 3");
        context.CreateLevel(user, "level 4");
        
        // test levels before start date
        context.Time.TimestampMilliseconds = 0;
        context.CreateLevel(user, "#ut before start");

        // test levels after end date
        context.Time.TimestampMilliseconds = 1000;
        context.CreateLevel(user, "#ut missed deadline");

        DatabaseList<GameLevel> levels = context.Database.GetLevelsFromContest(contest, 4, 0, user, new LevelFilterSettings(TokenGame.LittleBigPlanet2));
        Assert.That(levels.TotalItems, Is.EqualTo(3));
        Assert.That(levels.Items.All(l => l.Title.Contains("#ut ")));
    }
}