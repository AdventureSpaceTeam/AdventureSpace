generator-clogged = { $generator } резко отключается!
portable-generator-verb-start = Запустить генератор
portable-generator-verb-start-msg-unreliable = Запуск генератора. Это может потребовать нескольких попыток.
portable-generator-verb-start-msg-reliable = Запустить генератор.
portable-generator-verb-start-msg-unanchored = Генератор должен быть закреплён!
portable-generator-verb-stop = Остановить генератор
portable-generator-start-fail = Вы дёргаете за трос, но он не заводится.
portable-generator-start-success = Вы дёргаете за трос, и он оживает.
portable-generator-ui-title = Портативный генератор
portable-generator-ui-status-stopped = Остановлен:
portable-generator-ui-status-starting = Запускается:
portable-generator-ui-status-running = Работает:
portable-generator-ui-start = Старт
portable-generator-ui-stop = Стоп
portable-generator-ui-target-power-label = Цел. мощн. (кВт):
portable-generator-ui-efficiency-label = Эффективность:
portable-generator-ui-fuel-use-label = Расход топлива:
portable-generator-ui-fuel-left-label = Остаток топлива:
portable-generator-ui-clogged = В топливном баке обнаружено загрязнение!
portable-generator-ui-eject = Извлечь
portable-generator-ui-eta = (~{ $minutes } минут)
portable-generator-ui-unanchored = Не закреплено
portable-generator-ui-current-output = Current output: { $voltage }
power-switchable-generator-examine =
    Выработанная энергия направлена на { $output ->
        [HV] [color=orange]ВВ[/color]
       *[MV] [color=yellow]СВ[/color]
    }.
power-switchable-generator-switched = Switched output to { $voltage }!
power-switchable-voltage =
    { $voltage ->
        [HV] [color=orange]HV[/color]
        [MV] [color=yellow]MV[/color]
       *[LV] [color=green]LV[/color]
    }
power-switchable-switch-voltage = Switch to { $voltage }
fuel-generator-verb-disable-on = Turn the generator off first!
