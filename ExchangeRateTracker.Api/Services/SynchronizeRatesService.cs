﻿using ExchangeRateTracker.Api.Data;
using ExchangeRateTracker.Api.Exceptions;
using ExchangeRateTracker.Api.Models;
using ExchangeRateTracker.Api.Models.Dtos;
using ExchangeRateTracker.Api.Services.IServices;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExchangeRateTracker.Api.Services
{
    public class SynchronizeRatesService : ISynchronizeRatesService
    {
        private readonly AppDbContext _context;
        private readonly IBankApiService _bankApiService;
        private readonly List<string> _allowedCurrencies;

        public SynchronizeRatesService(AppDbContext context, IConfiguration configuration, IBankApiService bankApiService)
        {
            _context = context;
            _bankApiService = bankApiService;
            _allowedCurrencies = configuration.GetSection("AllowedCurrencies").Get<List<string>>();
        }

        public async Task SynchronizeByPeriodAsync(DateOnly dateFrom, DateOnly dateTo)
        {
            var ratesByYearDocs = await GetRatesByYearsAsync(GetYearsBetweenDates(dateFrom, dateTo).ToList());

            var rates = await ParseRatesFromManyDocsByPeriodAsync(ratesByYearDocs, dateFrom, dateTo);

            await AddOrUpdateRates(rates);
        }

        public async Task SynhronizeByDayAsync(DateOnly date)
        {
            var result = await _bankApiService.GetRatesByDayAsync(date);

            if (!result.IsSuccess)
                throw new SynchronizeException(result.Message);

            await AddOrUpdateRates(ApiResponseParserService.RatesByDate(result.Result, date));
        }

        /// <summary>
        /// Обновить курсы валют в БД
        /// </summary>
        /// <param name="rates"></param>
        /// <returns></returns>
        /// <exception cref="DbUpdateException"></exception>
        private async Task AddOrUpdateRates(List<ExchangeRate> rates)
        {
            try
            {
                //Обновить целиком данные в БД испоьзуя _context.ExchangeRates.UpdateRange() не получится, т.к. EF защищается от параллелизма и если запись
                //с ключом в БД уже есть, то в ней храниться версия и при обновлении строка с измененными данными должна содержать ту же версию. Поэтому нужно
                //каждую запись обрабатывать отдельно
                foreach (var rate in rates)
                {
                    var updRate = await _context.ExchangeRates.FirstOrDefaultAsync(r => r.CurrencyCode == rate.CurrencyCode && r.Date == rate.Date);

                    if (updRate == null)
                        await _context.AddAsync(rate);
                    else
                    {
                        updRate.Rate = rate.Rate;
                        updRate.Amount = rate.Amount;

                        _context.Update(updRate); 
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch
            {
                throw new DbUpdateException("Ошибка при обновлении курсов в БД");
            }
        }

        /// <summary>
        /// Получить список годов между двумя датами
        /// </summary>
        /// <param name="dateFrom">Дата с</param>
        /// <param name="dateTo">Дата по</param>
        /// <returns></returns>
        private IEnumerable<int> GetYearsBetweenDates(DateOnly dateFrom, DateOnly dateTo)
        {
            var currentYear = dateFrom.Year;

            while (currentYear <= dateTo.Year) 
            {
                yield return currentYear;
                currentYear++;
            }
        }

        /// <summary>
        /// Получить курсы валют по нескольким годам
        /// </summary>
        /// <param name="years">Список годов</param>
        /// <returns></returns>
        /// <exception cref="SynchronizeException"></exception>
        private async Task<List<string>> GetRatesByYearsAsync(List<int> years)
        {
            var listTasks = new List<Task<ResultDto>>();

            foreach (var year in years)
                listTasks.Add(_bankApiService.GetRatesByYearAsync(year));

            await Task.WhenAll(listTasks);

            if (listTasks.Any(task => task.Result.IsSuccess == false))
                throw new SynchronizeException("Ошибка при получении данных банка");

            return listTasks.Select(task => task.Result.Result).ToList();
        }

        /// <summary>
        /// Парсинг курсов валют из нескольких документов по курсам за год для получения курсов по определенному периоду времени
        /// </summary>
        /// <param name="ratesDocs">Документы с курсами валют</param>
        /// <param name="dateFrom">Дата с</param>
        /// <param name="dateTo">Дата по</param>
        /// <returns></returns>
        private async Task<List<ExchangeRate>> ParseRatesFromManyDocsByPeriodAsync(List<string> ratesDocs, DateOnly dateFrom, DateOnly dateTo)
        {
            var listParseTasks = new List<Task<List<ExchangeRate>>>();

            foreach (var ratesDoc in ratesDocs)
            {
                var task = Task.Run(() => ApiResponseParserService.RatesByPeriod(ratesDoc, dateFrom, dateTo, _allowedCurrencies));
                listParseTasks.Add(task);
            };
            
            await Task.WhenAll(listParseTasks);

            var rates = new List<ExchangeRate>();

            foreach (var task in listParseTasks)
                rates.AddRange(task.Result);

            return rates;
        }
    }
}
