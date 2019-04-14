using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpotifyAPI.Web.Example
{
    internal static class Program
    {
        private static string _clientId = ""; //"";
        private static string _secretId = ""; //"";
        private static bool end = false;

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {

            Environment.SetEnvironmentVariable("SPOTIFY_CLIENT_ID", "1f943e38b30c4a378c284f1ba0bafbf9");
            Environment.SetEnvironmentVariable("SPOTIFY_SECRET_ID", "7c100ccbb9714e13948a788b943636e8");

            _clientId = string.IsNullOrEmpty(_clientId)
                ? Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID")
                : _clientId;

            _secretId = string.IsNullOrEmpty(_secretId)
                ? Environment.GetEnvironmentVariable("SPOTIFY_SECRET_ID")
                : _secretId;

            Console.WriteLine("####### Spotify API Example #######");
            Console.WriteLine("This example uses AuthorizationCodeAuth.");
            Console.WriteLine(
                "Tip: If you want to supply your ClientID and SecretId beforehand, use env variables (SPOTIFY_CLIENT_ID and SPOTIFY_SECRET_ID)");

            AuthorizationCodeAuth auth = new AuthorizationCodeAuth(_clientId, _secretId, "http://localhost:4002", "http://localhost:4002",
                Scope.PlaylistReadPrivate | Scope.PlaylistReadCollaborative | Scope.AppRemoteControl | Scope.PlaylistModifyPrivate | Scope.PlaylistModifyPublic | Scope.UserReadPrivate | Scope.UserReadEmail | Scope.Streaming
                 | Scope.UserModifyPlaybackState | Scope.UserReadPlaybackState | Scope.UserReadRecentlyPlayed);
            auth.AuthReceived += AuthOnAuthReceived;
            auth.Start();
            auth.OpenBrowser();
            

            while(!end)
            {

            }

            //Thread.Sleep(1000000000);
            //Console.ReadLine();
            auth.Stop(0);
        }

        private static async void AuthOnAuthReceived(object sender, AuthorizationCode payload)
        {
            //testRead();
            AuthorizationCodeAuth auth = (AuthorizationCodeAuth)sender;
            auth.Stop();

            Token token = await auth.ExchangeCode(payload.Code);
            SpotifyWebAPI api = new SpotifyWebAPI
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };
            PrintUsefulData(api);
        }

        private static async void PrintUsefulData(SpotifyWebAPI api)
        {
            PrivateProfile profile = await api.GetPrivateProfileAsync();
            string name = string.IsNullOrEmpty(profile.DisplayName) ? profile.Id : profile.DisplayName;
            Console.WriteLine($"Hello there, {name}!");

            Console.WriteLine("Your playlists:");
            Paging<SimplePlaylist> playlists = await api.GetUserPlaylistsAsync(profile.Id);
            do
            {
                playlists.Items.ForEach(playlist =>
                {
                    Console.WriteLine($"- {playlist.Name}");
                });
                playlists = await api.GetNextPageAsync(playlists);
            } while (playlists.HasNextPage());

            SearchItem item = api.SearchItems("roadhouse+blues", SearchType.Album | SearchType.Playlist);
            Console.WriteLine(item.Albums.Total); //How many results are there in total? NOTE: item.Tracks = item.Artists = null
            Paging<SimpleAlbum> Albums = api.SearchItems("roadhouse+blues", SearchType.Album | SearchType.Playlist).Albums;
            do
            {
                Albums.Items.ForEach(SimpleAlbum =>
                {
                    Console.WriteLine($"- {SimpleAlbum.Name}");
                });
                Albums = await api.GetNextPageAsync(Albums);
            } while (Albums.HasNextPage());


            while (true)
            {


                Console.Write("\n\n\n");
                Console.WriteLine("Please Enter Song Query");
                string song = Console.ReadLine();
                if(song.Equals("end"))
                {
                    end = true;
                }
                song = song.Replace(" ", "+");
                SearchItem item2 = api.SearchItems(song, SearchType.Track);
                Console.WriteLine("Number of Results: ");
                Console.Write(item2.Tracks.Total + "\n"); //How many results are there in total? NOTE: item.Tracks = item.Artists = null
                Paging<FullTrack> Tracks = item2.Tracks;

                int i = 0;
                do
                {
                    Tracks.Items.ForEach(FullTrack =>
                    {
                        Console.WriteLine(i + $"- {FullTrack.Name}");
                        i++;
                    });
                    Tracks = await api.GetNextPageAsync(Tracks);
                } while (Tracks.HasNextPage());

                Console.WriteLine("Select Track (0-10)");
                int.TryParse(Console.ReadLine(), out int selectedTrack);

                Console.WriteLine("Please Select Device");
                AvailableDevices devices = api.GetDevices();

                devices.Devices.ForEach(device => Console.WriteLine(device.Name));
                int.TryParse(Console.ReadLine(), out int selectedDeviceInt);

                Device selectedDevice = devices.Devices[selectedDeviceInt];

                ErrorResponse error = api.ResumePlayback(selectedDevice.Id, null, uris: new List<string> { item2.Tracks.Items[selectedTrack].Uri.ToString() }, "", 0);

                Console.WriteLine(error.StatusCode());
            }
        }

    }
}