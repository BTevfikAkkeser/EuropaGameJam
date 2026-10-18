using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.UI;

public class FastestBoard : MonoBehaviour
{
    public const string LeaderboardId = "Leaderboard";

    const string NameKey = "cat-player-name";
    const string BestKey = "cat-fastest-ms";

    public static FastestBoard I { get; private set; }
    public static bool Blocking => I != null && I.card != null && I.card.activeSelf;

    [SerializeField] GameObject openButton;
    [SerializeField] GameObject card;
    [SerializeField] InputField nameField;
    [SerializeField] Text rows;
    [SerializeField] Text status;
    [SerializeField] Text notice;
    [SerializeField] GameObject noticeRoot;
    bool busy;

    void Awake()
    {
        I = this;
    }

    void OnDestroy()
    {
        if (I == this) I = null;
    }

    void Start()
    {
        if (nameField != null)
            nameField.text = PlayerPrefs.GetString(NameKey, "");
        _ = Boot();
    }

    public static void HidePicker()
    {
        if (I == null) return;
        if (I.openButton != null) I.openButton.SetActive(false);
        if (I.card != null) I.card.SetActive(false);
    }

    public static void Submit(float seconds)
    {
        if (I == null) return;
        I.Send(seconds);
    }

    async Task Boot()
    {
        SetStatus("Signing in...");
        if (!await Ready()) return;
        await PushName(PlayerPrefs.GetString(NameKey, ""));
        SetStatus("Ready.");
    }

    public void ToggleBoard()
    {
        bool show = card != null && !card.activeSelf;
        if (card != null) card.SetActive(show);
        if (show) _ = Refresh();
    }

    public void RefreshBoard() => _ = Refresh();

    public void CloseBoard()
    {
        if (card != null) card.SetActive(false);
    }

    public void SaveName()
    {
        _ = PushName(nameField != null ? nameField.text : "");
    }

    async void Send(float seconds)
    {
        int ms = Mathf.Max(1, Mathf.RoundToInt(seconds * 1000f));
        bool beatenLocal = !PlayerPrefs.HasKey(BestKey) || ms < PlayerPrefs.GetFloat(BestKey) - 0.5f;
        bool hadServer = false;
        double server = double.MaxValue;

        if (await Ready())
        {
            await PushName(PlayerPrefs.GetString(NameKey, ""));
            try
            {
                var mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(LeaderboardId);
                hadServer = true;
                server = mine.Score;
            }
            catch (Exception)
            {
                hadServer = false;
            }

            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, ms);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Leaderboard submit failed: " + e.Message);
                SetStatus(Explain(e.Message));
            }
        }

        bool beatenServer = !hadServer || ms < server - 0.5;
        if (beatenLocal && beatenServer)
        {
            PlayerPrefs.SetFloat(BestKey, ms);
            PlayerPrefs.Save();
            ShowNotice(seconds);
        }
    }

    async Task Refresh()
    {
        if (rows == null || busy) return;
        busy = true;
        rows.text = "Loading...";
        if (!await Ready())
        {
            rows.text = "";
            busy = false;
            return;
        }

        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, new GetScoresOptions { Limit = 10 });
            var builder = new StringBuilder();
            if (page.Results == null || page.Results.Count == 0)
                builder.Append("No times yet.");
            else
            {
                foreach (var entry in page.Results)
                {
                    builder.Append(entry.Rank + 1).Append(".  ");
                    builder.Append(Nick(entry.PlayerName));
                    builder.Append("    ");
                    builder.Append(PrettyMs(entry.Score));
                    builder.Append('\n');
                }
            }
            rows.text = builder.ToString();
            SetStatus("");
        }
        catch (Exception e)
        {
            rows.text = "";
            SetStatus(Explain(e.Message));
            Debug.LogWarning("Leaderboard read failed: " + e.Message);
        }

        busy = false;
    }

    async Task<bool> Ready()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            return true;
        }
        catch (Exception e)
        {
            SetStatus(Short(e.Message));
            Debug.LogWarning("Leaderboard sign-in failed: " + e.Message);
            return false;
        }
    }

    async Task PushName(string raw)
    {
        string name = Clean(raw);
        if (nameField != null && nameField.text != name)
            nameField.text = name;
        if (name.Length < 2)
        {
            if (!string.IsNullOrEmpty(raw))
                SetStatus("Use 2 to 16 letters or numbers. No spaces.");
            return;
        }

        PlayerPrefs.SetString(NameKey, name);
        PlayerPrefs.Save();
        if (!AuthenticationService.Instance.IsSignedIn) return;

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            SetStatus("Name saved.");
        }
        catch (Exception e)
        {
            SetStatus(Short(e.Message));
            Debug.LogWarning("Player name failed: " + e.Message);
        }
    }

    static string Nick(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return "Cat";
        int code = playerName.IndexOf('#');
        if (code >= 0) playerName = playerName.Substring(0, code);
        return playerName.Length == 0 ? "Cat" : playerName;
    }

    static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var builder = new StringBuilder(raw.Length);
        foreach (char c in raw.Trim())
        {
            if (char.IsLetterOrDigit(c)) builder.Append(c);
            if (builder.Length == 16) break;
        }
        return builder.ToString();
    }

    void ShowNotice(float seconds)
    {
        if (notice == null) return;
        notice.text = "New record   " + Pretty(seconds);
        if (noticeRoot != null) noticeRoot.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(HideNotice());
    }

    IEnumerator HideNotice()
    {
        yield return new WaitForSecondsRealtime(3.8f);
        if (noticeRoot != null) noticeRoot.SetActive(false);
    }

    void SetStatus(string message)
    {
        if (status != null) status.text = message;
    }

    static string Explain(string message)
    {
        if (!string.IsNullOrEmpty(message) && message.IndexOf("could not be found", StringComparison.OrdinalIgnoreCase) >= 0)
            return "Create a leaderboard with ID Leaderboard. Sort ascending, keep best.";
        return Short(message);
    }

    static string Short(string message)
    {
        if (string.IsNullOrEmpty(message)) return "Could not reach the board.";
        return message.Length > 90 ? message.Substring(0, 90) : message;
    }

    static string Pretty(float time)
    {
        if (time >= 60f)
        {
            int minutes = (int)time / 60;
            float seconds = time - minutes * 60f;
            return minutes + " min " + seconds.ToString("00") + " sec";
        }
        return time.ToString("0.0") + " seconds";
    }

    static string PrettyMs(double ms) => Pretty((float)(ms / 1000.0));
}
