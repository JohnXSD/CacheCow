﻿using CacheCow.Client;
using CacheCow.Client.Headers;
using CacheCow.Samples.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CacheCow.Samples.MvcCore
{
    class Program
    {
        private static TestServer _server;
        private static HttpClient _client;

        static void Main(string[] args)
        {
            // setup
            _server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>());
            var handler = _server.CreateHandler();

            _client = ClientExtensions.CreateClient(handler);
            _client.BaseAddress = _server.BaseAddress;

            Task.Run(RunAsync).Wait();

        }

        static async Task RunAsync()
        {
            await Menu();
        }

        static async Task Menu()
        {
            while(true)
            {
                Console.WriteLine(
@"CacheCow Cars Samples - (ASP.NET Core MVC and HttpClient)
    - Press 0 to list all cars
    - Press 1 to create a new car and add to repo
    - Press 2 to update the last item (updates last modified)
    - Press 3 to delete the last item
    - Press 4 to get the last item
    - Press x to exit
"
);
                var key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case 'x':
                        return;
                    case '0':
                        await ListAll();
                        break;
                    case '1':
                        await CreateNew();
                        break;
                    case '2':
                        await UpdateLast();
                        break;
                    case '3':
                        await DeleteLast();
                        break;
                    case '4':
                        await GetLast();
                        break;
                    default:
                        // nothing
                        break;
                }
            }
        }

        static async Task ListAll()
        {
            var response = await _client.GetAsync("/api/cars");
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync();
            WriteCacheCowHeader(response);
            Console.ForegroundColor = ConsoleColor.White;
            var cars = await response.Content.ReadAsAsync<IEnumerable<Car>>();

            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine($"| Id\t| NumberPlate\t| Year\t| Last Modified Date\t\t|");

            foreach (var c in cars)
            {
                Console.WriteLine($"| {c.Id}\t| {c.NumberPlate}\t| {c.Year}\t| {c.LastModified}\t|");
            }

            Console.WriteLine("-----------------------------------------------------------------");
            Console.ResetColor();
        }

        static async Task CreateNew()
        {
            var response = await _client.SendAsync( new HttpRequestMessage(HttpMethod.Post, "/api/car"));
            response.EnsureSuccessStatusCode();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Location header: {response.Headers.Location}");
            Console.WriteLine();
            Console.ResetColor();

        }

        static async Task UpdateLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if(id.HasValue)
            {
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/car/{id.Value}"));
                response.EnsureSuccessStatusCode();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        static async Task DeleteLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/car/{id.Value}"));
                response.EnsureSuccessStatusCode();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }

        }
        static async Task GetLast()
        {
            var id = InMemoryCarRepository.Instance.GetLastId();
            if (id.HasValue)
            {
                var response = await _client.GetAsync($"/api/car/{id.Value}");
                response.EnsureSuccessStatusCode();
                WriteCacheCowHeader(response);
                Console.ForegroundColor = ConsoleColor.White;
                var c = await response.Content.ReadAsAsync<Car>();
                Console.WriteLine($"| {c.Id}\t| {c.NumberPlate}\t| {c.Year}\t| {c.LastModified} |");
                Console.WriteLine();
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Repo is empty");
                Console.WriteLine();
                Console.ResetColor();
            }

        }

        static void WriteCacheCowHeader(HttpResponseMessage response)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Client: {response.Headers.GetCacheCowHeader()}");
            if(response.Headers.Contains(CacheCow.Server.Core.Headers.CacheCowHeader.Name))
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Server: " + 
                    response.Headers.GetValues(CacheCow.Server.Core.Headers.CacheCowHeader.Name).FirstOrDefault() ?? "");
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
