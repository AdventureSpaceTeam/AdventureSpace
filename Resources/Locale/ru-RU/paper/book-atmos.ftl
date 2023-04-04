book-text-atmos-distro =
    Сеть распределения, или же "дистра", жизненно важная для станции. Она отвечает за транспортировку воздуха с атмос-отдела по всей станции.
    
    Соответствующие трубы зачастую покрашены Выскакивающе-Приглушённым Синим, но безошибочный вариант определить их это использование т-лучевого сканера, чтобы отследить трубы подключённые к активным вентиляциям станции.
    
    Стандартная газовая смесь для сети распределения это 20 градусов по цельсию, 78% азота, 22% кислорода. Вы можете проверить это, используя газоанализатор на трубе дистры, или любой вентиляции подключённой к ней. Особые обстоятельства могут потребовать специальных смесей.
    
    Когда нужно думать над давлением дистры, есть несколько вещей которые нужно знать. Активные вентиляции регулируют давление на станции, так что пока всё работает стабильно, нет нужды в слишком высоком давлении дистры.
    
    Высокое давление дистры может сделать сеть "буфером" между газодобытчиками и вентиляциями, обеспечивая значительно количество воздуха, которое может использоваться для стабилизации давления на станции после разгерметизации.
    
    Низкое давление дистры уменьшит количество потерянного газа в случае разгерметизации сети, быстрый способ справится с загрязнением дистры. Так же это поможет уменьшить или предотвратить высокое давление на станции, в случае неполадок с вентиляциями.
    
    Обычное давление дистры в диапазоне 300-375 кПа, но другие давления могут быть использованы со знанием пользы и риска.
    
    Давление сети определяется последним насосом, который в неё закачивает. Для предотвращения заторов, все другие насосы между газодобытчиками и последним насосом должны стоять на максимальном значении, и все ненужные устройства должны быть убраны.
    
    Вы можете проверить давление дистры газоанализатором, но имейте в виду, что несмотря на заданное давление в трубах, разгерметизации могут вызвать недостаток давления в трубах на некоторое время. Так что если вы видите падение давления, не паникуйте - это может быть временно.
book-text-atmos-waste =
    Сеть отходов это основная система отвечающая за сохранения воздуха свободным от загрязнений.
    
    Вы можете распознать эти трубы по их Приятно-Тусклому Красному цвету или используя т-лучевой сканер, чтобы отследить какие трубы подсоединены к скрубберам на станции.
    
    Сеть отходов используется для транспортировки ненужных газов либо для фильтрации, либо для выброса в космос. Это идеально для поддержания давления на 0 кПа, но временами может быть низкое, не нулевое давление во время использования.
    
    Атмос техники могут выбрать фильтровать или выбрасывать газы в космос. Выбрасывание быстрее, но фильтрация позволяет повторно использовать или продавать газы.
    
    Сеть отходов может помочь диагностировать атмосферные проблемы на станции. Высокое количество отходов может указывать на большую утечку, в то время как присутствие не отходов может указывать на ошибку в конфигурации скруббера либо проблему с физическим подсоединением. Если у газов высокая температура, это может означать пожар.
book-text-atmos-alarms =
    Воздушные сигнализации расположены по всей станции для доступа к настройке и наблюдении за локальной атмосферой.
    
    Интерфейс воздушной сигнализации предоставляет атмос техникам список подключённых сенсоров, их показатели и возможность настроить пороги. Пороги используются для определения аварийного состояния воздушной тревоги. Атмос техники так же могут использовать интерфейс для установки целевого давления вентиляций, настройки рабочей скорости и целевых газов для скрубберов.
    
    Интерфейс позволяет не только точно настраивать все подключённые устройства, также доступно несколько режимов для быстрой настройки сигнализации. Эти режимы автоматически переключаются при изменении состояния тревоги:
    - Фильтрация: Обычный режим
    - Фильтрация (широкая): Режим фильтрации при котором скрубберы будут захватывать область побольше
    - Заполнение: Отключает скрубберы и ставит вентиляции на максимальное давление
    - Паника: Отключает вентиляции и ставит скрубберы на всасывание всего
    
    Мультитулом можно подключать устройства к воздушным сигнализациям.
book-text-atmos-vents =
    Ниже приведён краткое руководство по нескольким атмосферным устройствам.
    
    Пассивные вентиляции:
    Эти вентиляции не требуют питания, они позволяют газам свободно проходить как в трубопроводную сеть, к которой они присоединены, так и из неё.
    
    Активные вентиляции:
    Это самые распространённые вентиляции на станции. Они имеют встроенный насос и требуют электричества. По умолчанию они будут выкачивать газ из труб до 101 кПа. Однако они могут быть перенастроены, используя воздушные сигнализации. Так же они будут блокироваться, когда в комнате ниже 1 кПа для предотвращения выкачивания газов в космос.
    
    Скрубберы:
    Эти устройства позволяют убирать газы с окружающей среды в подсоединённую сеть труб. Они так же могут быть настроены для всасывания определённых газов, когда подключены к воздушной сигнализации.
    
    Инжекторы:
    Инжекторы подобны к активным вентиляциям, но они не имеют встроенного насоса и не требуют электричества. Их нельзя настроить, но они могут продолжать качать газы до очень высокого давления.
