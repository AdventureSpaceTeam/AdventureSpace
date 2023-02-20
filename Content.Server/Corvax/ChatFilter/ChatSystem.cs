﻿using System.Text.RegularExpressions;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private static readonly Dictionary<string, string> SlangReplace = new()
    {
        // Game
        { "хос", "гсб" },
        { "хоса", "гсб" },
        { "смо", "гв" },
        { "се", "си" },
        { "хоп", "гп" },
        { "хопа", "гп" },
        { "рд", "нр" },
        { "вард", "варден" },
        { "рофл", "прикол" },
        { "рофлишь", "шутишь" },
        { "кк", "красный код" },
        { "ск", "синий код" },
        { "зк", "зелёный код" },
        { "пда", "кпк" },
        { "корп", "корпоративный" },
        { "дэк", "детектив" },
        { "дэку", "детективу" },
        { "дэка", "детектива" },
        { "дек", "детектив" },
        { "деку", "детективу" },
        { "дека", "детектива" },
        { "мш", "имплант защиты разума" },
        { "трейтор", "предатель" },
        { "инжи", "инженеры" },
        { "инжинер", "инженер" },
        { "яо", "яой" }, // braindead
        { "нюк", "ядерный оперативник" },
        { "нюкеры", "ядерные оперативники" },
        { "нюкер", "ядерный оперативник" },
        { "нюкеровец", "ядерный оперативник" },
        { "аирлок", "шлюз" },
        { "аирлоки", "шлюзы" },
        { "айрлок", "шлюз" },
        { "айрлоки", "шлюзы" },
        { "визард", "волшебник" },
        { "дизарм", "толкнуть" },
        { "синга", "сингулярность" },
        { "сингу", "сингулярность" },
        // IC
        { "норм", "нормально" },
        { "хз", "не знаю" },
        { "синд", "синдикат" },
        { "синди", "синдикат" },
        { "пон", "понятно" },
        { "непон", "не понятно" },
        { "нипон", "не понятно" },
        { "кста", "кстати" },
        { "кст", "кстати" },
        { "плз", "пожалуйста" },
        { "пж", "пожалуйста" },
        { "спс", "спасибо" },
        { "сяб", "спасибо" },
        { "прив", "привет" },
        { "ок", "окей" },
        { "чел", "мужик" },
        { "лан", "ладно" },
        { "збс", "заебись" },
        { "мб", "может быть" },
        { "оч", "очень" },
        { "омг", "боже мой" },
        { "нзч", "не за что" },
        { "пок", "пока" },
        { "бб", "пока" },
        { "пох", "плевать" },
        { "ясн", "ясно" },
        { "всм", "всмысле" },
        { "чзх", "что за херня?" },
        { "изи", "легко" },
        { "гг", "хорошо сработано" },
        { "пруф", "доказательство" },
        { "пруфани", "докажи" },
        { "пруфанул", "доказал" },
        { "брух", "мда..." },
        { "имба", "нечестно" },
        { "разлокать", "разблокировать" },
        { "юзать", "использовать" },
        { "юзай", "используй" },
        { "юзнул", "использовал" },
        { "хилл", "лечение" },
        { "подхиль", "полечи" },
        { "хильни", "полечи" },
        { "хелп", "помоги" },
        { "хелпани", "помоги" },
        { "хелпанул", "помог" },
        // OOC
        { "афк", "ссд" },
        { "админ", "бог" },
        { "админы", "боги" },
        { "админов", "богов" },
        { "забанят", "покарают" },
        { "бан", "наказание" },
        { "пермач", "наказание" },
        { "перм", "наказание" },
        { "запермили", "наказание" },
        { "запермят", "накажут" },
        { "нонрп", "плохо" },
        { "нрп", "плохо" },
        { "рдм", "плохо" },
        { "дм", "плохо" },
        { "гриф", "плохо" },
        { "фрикил", "плохо" },
        { "фрикилл", "плохо" },
        { "лкм", "левая рука" },
        { "пкм", "правая рука" },
    };

    private string ReplaceWords(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        return Regex.Replace(message, "\\b(\\w+)\\b", match =>
        {
            if (SlangReplace.TryGetValue(match.Value.ToLower(), out var replacement))
                return replacement;
            return match.Value;
        });
    }
}
