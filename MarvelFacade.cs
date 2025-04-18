﻿using HeroExplorer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Net.Http;
using System.Collections.ObjectModel;

namespace HeroExplorer
{
    public class MarvelFacade
    {
        private const string PrivateKey = "13afd975425abdaf29a013f26ef12789ed536c06";
        private const string PublicKey = "4b76a06259953851972ad8977efec731";
        private const string ImageNotAvailablePath = "http://i.annihil.us/u/prod/marvel/i/mg/b/40/image_not_available";
        private const int MaxCharacters = 1500;

        public static async Task PopulateMarvelCharactersAsync(ObservableCollection<Character> marvelCharacters)
        {
            try
            {
                var characterDataWrapper = await GetCharacterDataWrapperAsync();

                var characters = characterDataWrapper.data.results;

                foreach (var character in characters)
                {
                    if (character.thumbnail != null
                        && character.thumbnail.path != ""
                        && character.thumbnail.path != ImageNotAvailablePath)
                    {

                        character.thumbnail.small = String.Format("{0}/standard_small.{1}",
                            character.thumbnail.path,
                            character.thumbnail.extension);

                        character.thumbnail.large = String.Format("{0}/portrait_xlarge.{1}",
                            character.thumbnail.path,
                            character.thumbnail.extension);

                        marvelCharacters.Add(character);
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }


        public static async Task PopulateMarvelComicsAsync(int characterId, ObservableCollection<ComicBook> marvelComics)
        {
            try
            {
                var comicDataWrapper = await GetComicDataWrapperAsync(characterId);

                var comics = comicDataWrapper.data.results;

                foreach (var comic in comics)
                {
                    if (comic.thumbnail != null
                        && comic.thumbnail.path != ""
                        && comic.thumbnail.path != ImageNotAvailablePath)
                    {

                        comic.thumbnail.small = String.Format("{0}/portrait_medium.{1}",
                            comic.thumbnail.path,
                            comic.thumbnail.extension);

                        comic.thumbnail.large = String.Format("{0}/portrait_xlarge.{1}",
                            comic.thumbnail.path,
                            comic.thumbnail.extension);

                        marvelComics.Add(comic);
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }



        private static async Task<CharacterDataWrapper> GetCharacterDataWrapperAsync()
        {
            Random random = new Random();
            var offset = random.Next(MaxCharacters);

            string url = String.Format("http://gateway.marvel.com:80/v1/public/characters?limit=10&offset={0}",
                offset);

            var jsonMessage = await CallMarvelAsync(url);
            var serializer = new DataContractJsonSerializer(typeof(CharacterDataWrapper));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));

            var result = (CharacterDataWrapper)serializer.ReadObject(ms);
            return result;
        }

        private static async Task<ComicDataWrapper> GetComicDataWrapperAsync(int characterId)
        {
            var url = String.Format("http://gateway.marvel.com:80/v1/public/comics?characters={0}&limit=10",
                characterId);
            var jsonMessage = await CallMarvelAsync(url);

            var serializer = new DataContractJsonSerializer(typeof(ComicDataWrapper));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonMessage));

            var result = (ComicDataWrapper)serializer.ReadObject(ms);
            return result;
        }

        private async static Task<string> CallMarvelAsync(string url)
        {
            var timeStamp = DateTime.Now.Ticks.ToString();
            var hash = CreateHash(timeStamp);

            string completeUrl = String.Format("{0}&apikey={1}&ts={2}&hash={3}", url, PublicKey, timeStamp, hash);
            HttpClient http = new HttpClient();
            var response = await http.GetAsync(completeUrl);
            return await response.Content.ReadAsStringAsync();
        }

        private static string CreateHash(string timeStamp)
        {

            var toBeHashed = timeStamp + PrivateKey + PublicKey;
            var hashedMessage = ComputeMD5(toBeHashed);
            return hashedMessage;
        }

        private static string ComputeMD5(string str)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

    }
}