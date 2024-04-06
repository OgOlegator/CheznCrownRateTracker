# ExchangeRateTracker

Чешский национальный банк предоставляет возможность отслеживать валютный курс чешской кроны.
Ежедневный курс доступен по адресу: https://www.cnb.cz/en/financial_markets/foreign_exchange_market/exchange_rate_fixing/daily.txt?date=27.07.2019
Исторические данные доступны по адресу: https://www.cnb.cz/en/financial_markets/foreign_exchange_market/exchange_rate_fixing/year.txt?year=2019

## Задание
Необходимо разработать программное решение, обладающее следующей функциональностью:
1) синхронизация данных по чешской кроне за текущую дату в БД по расписанию. Должна быть возможность сконфигурировать время/интервал запуска. Например: запускать синхронизацию каждый день в 0:01. Период запуска должен задаваться конфигурации приложения.
2) синхронизация данных по чешской кроне за период времени. На вход подается startDate и endDate, приложение синхронизирует в БД данные за этот период. Валюты, по которым синхронизируются данные, должны быть в конфигурации приложения. 
3) предоставляет web-API, с помощью которого можно получить отчет по курсу кроны за период времени. В отчете необходимо вывести минимальное, максимальное и среднее значение каждой из выбранных валют отдельно. Валюты, по которым строится отчёт, передаются в запросе. Показатели в отчёте необходимо рассчитывать для валюты в количестве 1 условная единица, т.е. для Amount = 1. Формат отчета – JSON.

* Необходимо учесть, что в данных, предоставляемых API, могут быть аномалии. Например, для некоторых временных интервалов может не быть курсов определенных валют и т.п.

## Стек:
- .Net Core 6
- .Net Framework 4.7.2
- Web Api
- Windows Service
- MsSql
- Ef Core

## Описание приложения
Приложение состоит из 3х компонентов: 
- Web Api, который позволяет запускать синхронизацию курсов валют и строить отчеты по валютам;
- Windows Service для автоматического запуска синхронизации курсов за текущий день;
- БД (MsSql).

## Конфигурация приложения

### Подключение к БД
- В файл appsettings.json необходимо добавить строку подключения:
"ConnectionStrings": {
  "DefaultConnection": "Server=<name_your_server_db>\\SQLEXPRESS;Database=ExchangeRateTracker;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true"
}
- Выполнить миграцию, например, в "Консоли диспетчера пакетов" в VS:
Update-database

### Windows service
- Запустить PowerShell от имени администратора и выполнить команду:
New-Service -Name "AutoSynhronizeExchRate" -BinaryPathName <Полный путь к файлу с проектом>\ExchangeRateTracker\ExchangeRateTracker.AutoSynhronize\bin\Debug\ExchangeRateTracker.AutoSynhronize.exe
- Зайти в приложение "Службы", найти службу AutoSynhronizeExchRate и запустить.

