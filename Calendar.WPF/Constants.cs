﻿namespace Calendar.WPF;

public static class Constants
{
    public static readonly IReadOnlyDictionary<(int Month, int Day), string> MonthAndDayToHoliday = new Dictionary<(int Month, int Day), string>
    {
        { (1, 1), "Новый год" },
        { (1, 7), "Рождество Христово" },
        { (1, 14), "Старый Новый год" },
        { (1, 25), "Татьянин день" },
        { (2, 14), "День святого Валентина" },
        { (2, 23), "День защитника Отечества" },
        { (3, 8), "Международный женский день" },
        { (4, 1), "День смеха" },
        { (4, 12), "День космонавтики" },
        { (5, 1), "Праздник весны и труда" },
        { (5, 9), "День Победы" },
        { (6, 1), "Международный день защиты детей" },
        { (6, 12), "День России" },
        { (7, 8), "День семьи, любви и верности" },
        { (8, 22), "День государственного флага РФ" },
        { (9, 1), "День знаний" },
        { (9, 27), "День воспитателя и всех дошкольных работников" },
        { (10, 4), "Всемирный день животных" },
        { (10, 31), "Хэллоуин" },
        { (11, 4), "День народного единства" },
        { (11, 10), "Всемирный день науки" },
        { (11, 25), "День матери в России" },
        { (12, 25), "Католическое Рождество" },
        { (5, 15), "Международный день семьи" },
        { (6, 5), "Всемирный день окружающей среды" },
        { (9, 8), "Международный день грамотности" },
        { (10, 5), "День учителя" },
    };
}