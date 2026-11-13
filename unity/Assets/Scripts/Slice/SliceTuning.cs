using System;
using UnityEngine;

namespace GameDev.Slice
{
    /// <summary>
    /// The owner's tuning page (owner, 2026-11-12: the values of feel are taken out where the owner turns them in Unity,
    /// grouped by what they do and named for it). One asset, <c>Assets/Resources/SliceTuning.asset</c>, opened by
    /// <c>GameDev/Slice/Tuning</c>: a change made in Play mode stays after it, as an asset's does, and the next jump, stroke
    /// or frame reads it. Every value here is an authoring default until the owner sets it. Code reads the page through
    /// <see cref="Current"/>, which keeps these defaults when the asset is missing.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Slice/Tuning", fileName = "SliceTuning")]
    public sealed class SliceTuning : ScriptableObject
    {
        /// <summary>The asset's name under a Resources folder.</summary>
        public const string ResourceName = "SliceTuning";

        [Serializable]
        public sealed class Landing
        {
            [Tooltip("Секунды, которые колени держатся согнутыми после прыжка; потом нажатая клавиша сразу пускает дальше. " +
                     "Владелец, 2026-11-12: ещё вдвое короче, 0.15.")]
            [Min(0f)]
            public float KneesHeldSeconds = 0.15f;

            [Tooltip("Какую долю клипа приземления персонаж доигрывает, когда не нажато ни одной клавиши, от 0 до 1.")]
            [Range(0f, 1f)]
            public float PlayedShareWithoutKeys = 0.3f;

            [Tooltip("Секунды в воздухе, после которых и падение с края приземляется в колени; прыжок приземляется в колени всегда.")]
            [Min(0f)]
            public float FallBeforeKneesSeconds = 0.45f;
        }

        [Serializable]
        public sealed class WorkTiming
        {
            [Tooltip("Секунды от начала клипа, где клип замирает и появляется полоса проверки реакции. " +
                     "Персонаж замирает именно в этой позе: больше — проверка позже по взмаху.")]
            [Min(0f)]
            public float CheckAtSeconds;

            [Tooltip("Секунды от начала клипа, с которых играется первый взмах; 0 — с начала клипа. " +
                     "Если это позже проверки, первая проверка приходит во втором круге клипа.")]
            [Min(0f)]
            public float FirstStrokeFromSeconds;

            public WorkTiming()
            {
            }

            public WorkTiming(float checkAt, float firstStrokeFrom = 0f)
            {
                CheckAtSeconds = checkAt;
                FirstStrokeFromSeconds = firstStrokeFrom;
            }
        }

        [Serializable]
        public sealed class ToolGrip
        {
            [Tooltip("Сдвиг инструмента, метры, в осях кисти: двигает весь инструмент в ладони.")]
            public Vector3 ShiftInHandMetres;

            [Tooltip("Доворот инструмента, градусы, вокруг его собственных осей (X, Y, Z по очереди).")]
            public Vector3 TurnDegrees;
        }

        [Serializable]
        public sealed class Interface
        {
            [Tooltip("Множитель размера всего интерфейса: 1 — как задумано (1920×1080), меньше 1 — мельче и больше помещается.")]
            [Range(0.5f, 1.5f)]
            public float Scale = 1f;

            [Tooltip("По какой стороне окна подгоняется интерфейс: 0 — по ширине (как было), 1 — по высоте, между ними — смесь. " +
                     "В окне шире 16:9 значение ближе к 1 вмещает всё по высоте.")]
            [Range(0f, 1f)]
            public float FitToHeight;
        }

        [Serializable]
        public sealed class Aiming
        {
            [Tooltip("Секунды, за которые оружие поднимается к глазу после нажатия правой кнопки мыши и опускается после.")]
            [Min(0.01f)]
            public float RaiseSeconds = 0.25f;

            [Tooltip("Угол обзора камеры в прицеле с пистолетом, градусы по вертикали: меньше — ближе и уже. Обычный взгляд — 60.")]
            [Range(10f, 60f)]
            public float PistolFieldOfView = 40f;

            [Tooltip("Угол обзора камеры в прицеле с винтовкой, градусы по вертикали: меньше — ближе и уже. Обычный взгляд — 60.")]
            [Range(10f, 60f)]
            public float RifleFieldOfView = 30f;

            [Tooltip("Метры от глаза до целика пистолета: насколько далеко вытянуты руки. Больше — оружие меньше на экране.")]
            [Range(0.05f, 1f)]
            public float PistolEyeToRearSight = 0.4f;

            [Tooltip("Метры от глаза до целика винтовки: приклад у щеки. Больше — оружие меньше на экране.")]
            [Range(0.05f, 1f)]
            public float RifleEyeToRearSight = 0.55f;

            [Tooltip("Где стоит глаз, то есть камера в прицеле, относительно кости головы, метры: X вправо, Y вверх, Z вперёд " +
                     "по направлению тела. Вперёд — чтобы голова не попадала в кадр.")]
            public Vector3 EyeFromHead = new Vector3(0.03f, 0.09f, 0.12f);

            [Tooltip("Секунды сглаживания глаза: насколько шаги и покачивание тела качают вид в прицеле. 0 — глаз движется с головой.")]
            [Range(0f, 0.3f)]
            public float EyeSmoothingSeconds = 0.08f;

            [Tooltip("Чувствительность мыши в прицеле, доля обычной: меньше — точнее наводка.")]
            [Range(0.1f, 1f)]
            public float MouseShare = 0.6f;

            [Tooltip("Скорость пули в полёте, м/с: так быстро, чтобы её было видно. Настоящая пуля летит в несколько раз быстрее.")]
            [Range(10f, 400f)]
            public float BulletSpeed = 80f;

            [Tooltip("Какая доля силы тяжести тянет пулю вниз: 0 — летит прямо, 1 — падает как камень при этой скорости. " +
                     "0.05 даёт падение настоящей пули: около 1.5 см на 20 м.")]
            [Range(0f, 1f)]
            public float BulletGravityShare = 0.05f;

            [Tooltip("Метры, после которых пуля, никого не задев, пропадает.")]
            [Range(20f, 400f)]
            public float BulletReach = 150f;
        }

        [Header("Прыжок и приземление")]
        public Landing Jump = new Landing();

        [Header("Прицел и выстрел вне боя (правая кнопка мыши)")]
        public Aiming Aim = new Aiming();

        [Header("Работа: в какой момент клипа замирает персонаж и появляется проверка реакции")]
        [Tooltip("Рубка дерева топором (клип Chop). Удар топора о ствол, по замеру, на 0.18 с.")]
        public WorkTiming Chopping = new WorkTiming(0.17f, 0.6f);

        [Tooltip("Добыча руды киркой (клип Mine). Удар о землю, по замеру, на 0.40 с.")]
        public WorkTiming Mining = new WorkTiming(0.35f);

        [Tooltip("Последний удар молотом на наковальне (клип HammerDown). Молот встречает наковальню, по замеру, на 0.38 с.")]
        public WorkTiming Forging = new WorkTiming(0.35f);

        [Tooltip("Сбор трав с грядки (клип Harvest).")]
        public WorkTiming HerbPicking = new WorkTiming(1.27f);

        [Tooltip("Работа на коленях: свежевание туши и первая помощь (клип Kneel).")]
        public WorkTiming Kneeling = new WorkTiming(4.30f);

        [Tooltip("Кража из кармана (клип Gather2).")]
        public WorkTiming Pickpocketing = new WorkTiming(0.60f);

        [Tooltip("Варка зелья за алхимическим столом (клип Interact): каждая из шести проверок. Левая рука дальше всего, по замеру, на 0.77 с.")]
        public WorkTiming Brewing = new WorkTiming(0.77f);

        [Header("Инструменты в руке: поправка к хвату, видна сразу во время работы")]
        public ToolGrip Axe = new ToolGrip();

        public ToolGrip Pickaxe = new ToolGrip();

        public ToolGrip Hammer = new ToolGrip();

        [Header("Пропуск времени")]
        [Tooltip("Усвоение зелья Предела: сколько тиков мира (игровых минут) проходит за секунду, пока персонаж сидит на корточках, а мир идёт. 60 тиков при 10 — шесть секунд.")]
        [Range(1f, 60f)]
        public float AssimilationTicksPerSecond = 10f;

        [Tooltip("Сон: сколько тиков мира (игровых минут) проходит за секунду, пока персонаж спит, а мир идёт. 360 тиков при 60 — шесть секунд.")]
        [Range(1f, 240f)]
        public float SleepTicksPerSecond = 60f;

        [Header("Рассудок")]
        [Tooltip("Шёпот, от лёгкого смятения и ниже: через сколько секунд он слышится снова — не раньше X и не позже Y.")]
        public Vector2 WhisperSeconds = new Vector2(35f, 80f);

        [Tooltip("Тень на краю экрана, от сильного смятения и ниже: через сколько секунд — не раньше X и не позже Y.")]
        public Vector2 GlimpseSeconds = new Vector2(50f, 110f);

        [Tooltip("Сколько секунд тень стоит, если камера не повернулась.")]
        [Range(1f, 10f)]
        public float GlimpseHold = 4f;

        [Tooltip("Ложная тревога, от тяжёлого смятения и ниже: шкала замечания заполняется ни от кого; через сколько секунд — не раньше X и не позже Y.")]
        public Vector2 FalseAlarmSeconds = new Vector2(70f, 140f);

        [Tooltip("На грани: шёпот поверх реплик разговора; через сколько секунд — не раньше X и не позже Y.")]
        public Vector2 DialogueWhisperSeconds = new Vector2(5f, 10f);

        [Tooltip("Громкость шёпота, от 0 до 1.")]
        [Range(0f, 1f)]
        public float WhisperVolume = 0.3f;

        [Tooltip("На грани мир теряет цвет: насыщенность, от 0 (как есть) до −100 (серый мир).")]
        [Range(-100f, 0f)]
        public float ColourDrain = -85f;

        [Header("Интерфейс")]
        public Interface Ui = new Interface();

        private static SliceTuning _current;

        /// <summary>The project's page, or a page of the defaults when it is missing.</summary>
        public static SliceTuning Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<SliceTuning>(ResourceName);
                    if (_current == null)
                    {
                        _current = CreateInstance<SliceTuning>();
                        _current.hideFlags = HideFlags.DontSave;
                    }
                }

                return _current;
            }
        }

        /// <summary>A work clip's timing, by the clip's state in the player's controller; null for a clip with none.</summary>
        public WorkTiming Work(string clip)
        {
            switch (clip)
            {
                case "Chop":
                    return Chopping;
                case "Mine":
                    return Mining;
                case "HammerDown":
                    return Forging;
                case "Harvest":
                    return HerbPicking;
                case "Kneel":
                    return Kneeling;
                case "Gather2":
                    return Pickpocketing;
                case "Interact":
                    return Brewing;
                default:
                    return null;
            }
        }

        /// <summary>A tool's grip, by its child's name in the hand; null for another.</summary>
        public ToolGrip Grip(string tool)
        {
            switch (tool)
            {
                case PlayerWork.AxeTool:
                    return Axe;
                case PlayerWork.PickaxeTool:
                    return Pickaxe;
                case PlayerWork.HammerTool:
                    return Hammer;
                default:
                    return null;
            }
        }
    }
}
