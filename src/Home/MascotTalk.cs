// Stackshot - What the mascot says, by personality.
// MIT License - https://github.com/rubenitx/stackshot
using System;
using System.Collections.Generic;

namespace Stackshot
{
    // Lines are picked at random but never repeat one said recently, so it does not sound like a loop.
    public static class MascotTalk
    {
        static readonly Random rnd = new Random();
        static readonly List<string> recent = new List<string>();

        // "3 glasses", "1 break": the count with its noun in the right form for the language (keys: one, many; the 2-4 form of Ukrainian is the many key + " [2-4]").
        public static string Count(int n, string one, string many)
        {
            return string.Format(L.Culture, L.Plural(n, one, many + " [2-4]", many), n);
        }

        static string Pick(string[] lines)
        {
            string line = null;
            for (int i = 0; i < 10; i++)
            {
                line = lines[rnd.Next(lines.Length)];
                if (!recent.Contains(line)) break;
            }
            recent.Add(line);
            if (recent.Count > 24) recent.RemoveAt(0);
            return line;
        }

        // Three voices: 0 cheerful, 1 calm, 2 cheeky.
        static string By(int personality, string[] cheerful, string[] calm, string[] cheeky)
        {
            return Pick(personality == 1 ? calm : personality == 2 ? cheeky : cheerful);
        }

        public static string Greeting()
        {
            int h = DateTime.Now.Hour;
            return h < 6 ? L.T("\u00A1Buenas noches!") : h < 14 ? L.T("\u00A1Buenos d\u00EDas!") : h < 21 ? L.T("\u00A1Buenas tardes!") : L.T("\u00A1Buenas noches!");
        }

        public static string Hello(Settings s)
        {
            string g = Greeting();
            switch (s.MascotPersonality)
            {
                case 1: return g + " " + Pick(new string[] { L.T("Aqu\u00ED estoy, tranquilo."), L.T("Sin prisa, ya sabes d\u00F3nde encontrarme."), L.T("Todo en calma por aqu\u00ED.") });
                case 2: return g + " " + Pick(new string[] { L.T("\u00BFA qui\u00E9n capturamos hoy?"), L.T("Ya era hora de que aparecieras."), L.T("\u00BFQu\u00E9 liamos hoy, jefe?") });
                default: return g + " " + Pick(new string[] { L.T("\u00BFQu\u00E9 capturamos?"), L.T("\u00BFListo para una buena captura?"), L.T("\u00A1Qu\u00E9 ganas ten\u00EDa de verte!") });
            }
        }

        // Sometimes one of the character's own lines (null otherwise, or when it has none).
        static string Own(Settings s, double chance)
        {
            string[] lines = MascotParts.OwnLines(MascotParts.KindLatest(s.MascotKind));   // a body redrawn since speaks as the new one
            return lines != null && rnd.NextDouble() < chance ? Pick(lines) : null;
        }

        public static string[] Pokes(Settings s)
        {
            string own = Own(s, 0.5);
            if (own != null) return new string[] { own };
            string key = Hotkeys.Display(s.HotRegion);
            switch (s.MascotPersonality)
            {
                case 1:
                    return new string[] { Pick(new string[] { L.T("Mmm\u2026 hola."), L.T("Con calma, que hay tiempo."), L.T("Respira. Captura. Repite."), L.F("Pulsa {0} cuando quieras.", key),
                                                              L.T("Un toquecito, qu\u00E9 detalle."), L.T("Aqu\u00ED sigo, sin prisa."), L.T("Todo a su ritmo."), L.T("Qu\u00E9 bien se est\u00E1 aqu\u00ED.") }) };
                case 2:
                    return new string[] { Pick(new string[] { L.T("\u00A1Eh! \u00A1Que no soy un bot\u00F3n!"), L.T("\u00BFOtra vez t\u00FA?"), L.T("Si me pinchas m\u00E1s, me pongo a bailar."),
                                                              L.T("Yo hago las capturas m\u00E1s chulas, que lo sepas."), L.F("Pulsa {0} y no me hagas cosquillas.", key),
                                                              L.T("Cuidado, que muerdo (un poquito)."), L.T("\u00BFTe aburres? Se nota."), L.T("Me debes una captura."),
                                                              L.T("Soy un artista, no un juguete."), L.T("Mira que eres pesado\u2026 me gusta.") }) };
                default:
                    return new string[] { Pick(new string[] { L.T("\u00A1Eh, que me haces cosquillas!"), L.T("\u00BFHacemos una captura?"), L.F("Pulsa {0} y yo me encargo.", key),
                                                              L.T("\u00A1Sonr\u00EDe! Bueno\u2026 yo ya lo hago."), L.T("Bip bup. Bip."), L.T("Me encanta cuando curvas las flechas."),
                                                              L.T("\u00A1Hola, hola!"), L.T("\u00A1Otra vez! \u00A1Otra vez!"), L.T("Jijiji, para, para."), L.T("\u00A1Qu\u00E9 d\u00EDa tan bueno para capturar!") }) };
            }
        }

        public static string Tip(Settings s)
        {
            string[] tips =
            {
                L.T("Arrastra una miniatura a cualquier chat para pegarla."),
                L.T("En el editor, tira del punto azul de una flecha para curvarla."),
                L.T("Con la rueda del rat\u00F3n sobre la pila ves las capturas anteriores."),
                L.T("Al capturar, un clic en una ventana la saca enterita."),
                L.T("En el editor, Enter copia y cierra."),
                // A cleared combination would read "Sin atajo captura...": then the tip names the action instead.
                Hotkeys.Split(s.HotScroll).Count > 0 ? L.F("{0} captura una p\u00E1gina entera desplaz\u00E1ndola.", Hotkeys.Display(s.HotScroll))
                                                     : L.T("La captura con desplazamiento saca una p\u00E1gina entera; en Atajos puedes darle una combinaci\u00F3n."),
                Hotkeys.Split(s.HotText).Count > 0 ? L.F("{0} copia el texto de cualquier parte de la pantalla, sin guardar captura.", Hotkeys.Display(s.HotText))
                                                   : L.T("Copiar texto de la pantalla saca el texto de cualquier sitio; en Atajos puedes darle una combinaci\u00F3n."),
                Hotkeys.Split(s.HotRepeat).Count > 0 ? L.F("{0} vuelve a capturar la \u00FAltima \u00E1rea, sin elegirla otra vez.", Hotkeys.Display(s.HotRepeat))
                                                     : L.T("\u00ABRepetir \u00E1rea\u00BB, en la bandeja, vuelve a capturar la \u00FAltima \u00E1rea; en Atajos puedes darle una combinaci\u00F3n."),
                L.T("El bot\u00F3n Fondo del editor deja tus capturas listas para presentar."),
                L.T("Para tapar datos, usa Tapar en el editor: un bloque s\u00F3lido no se puede deshacer."),
                L.T("En la secci\u00F3n Mascota puedes cambiarme el gorro, la ropa y hasta la especie."),
                L.T("Si me activas en el escritorio, te acompa\u00F1o junto a la barra de tareas."),
                L.T("Puedes grabar la pantalla en v\u00EDdeo desde mi men\u00FA."),
                L.T("Si me arrastras, me puedes dejar encima de cualquier ventana."),
                L.T("Un doble clic r\u00E1pido sobre m\u00ED y me pongo a hacer cosas."),
                L.T("Las capturas se guardan solas: \u00E1brelas desde Abrir mis capturas.")
            };
            return Pick(tips);
        }

        public static string Celebrate(Settings s)
        {
            return By(s.MascotPersonality,
                new string[] { L.T("\u00A1Buena captura!"), L.T("\u00A1Clic! Ha quedado genial."), L.T("Esa me la guardo."), L.T("\u00A1Qu\u00E9 buen ojo!"), L.T("\u00A1Perfecta!"), L.T("\u00A1Otra para la colecci\u00F3n!") },
                new string[] { L.T("Bonita captura."), L.T("Guardada en mi memoria."), L.T("Muy bien."), L.T("Esa tiene buena luz."), L.T("Todo en su sitio."), L.T("Bien encuadrada.") },
                new string[] { L.T("\u00A1Toma ya!"), L.T("\u00A1Eso es arte!"), L.T("\u00A1Directa al museo!"), L.T("Y yo sin cobrar derechos."), L.T("Esa va a enmarcarse."), L.T("\u00BFVes? Te lo dije.") });
        }

        // Spontaneous lines on the desktop: a mix of its voice, the time of day and the odd tip.
        public static string Idle(Settings s)
        {
            int h = DateTime.Now.Hour;
            double r = rnd.NextDouble();
            if (r < 0.18) return Tip(s);
            string own = Own(s, 0.35);
            if (own != null) return own;
            if (r < 0.38)
            {
                if (h >= 7 && h < 11) return By(s.MascotPersonality, new string[] { L.T("\u00A1Ese caf\u00E9 de la ma\u00F1ana!"), L.T("Buen momento para empezar fuerte."), L.T("\u00A1Arrancamos el d\u00EDa!") },
                    new string[] { L.T("La ma\u00F1ana va tranquila."), L.T("Con calma se empieza mejor."), L.T("Un d\u00EDa a la vez.") },
                    new string[] { L.T("\u00BFYa has desayunado o qu\u00E9?"), L.T("Esos ojos dicen \u00ABfalta caf\u00E9\u00BB."), L.T("Primeras horas, primeros memes.") });
                if (h >= 13 && h < 16) return By(s.MascotPersonality, new string[] { L.T("\u00BFYa has comido?"), L.T("Se acerca la hora de la siesta."), L.T("\u00A1A por la tarde!") },
                    new string[] { L.T("Despu\u00E9s de comer, paseo corto."), L.T("La sobremesa es sagrada."), L.T("Qu\u00E9 sue\u00F1ecito\u2026") },
                    new string[] { L.T("\u00BFY mi parte de la comida?"), L.T("Tras comer, ni los bots rinden."), L.T("Ojo con la siesta, jefe.") });
                if (h >= 19 || h < 6) return By(s.MascotPersonality, new string[] { L.T("\u00BFA\u00FAn trabajando? \u00A1\u00C1nimo!"), L.T("Ya queda menos para cerrar."), L.T("\u00A1Qu\u00E9 horas!") },
                    new string[] { L.T("Ya es tarde. Descansa un poco."), L.T("Los ojos tambi\u00E9n cansan."), L.T("Ma\u00F1ana ser\u00E1 otro d\u00EDa.") },
                    new string[] { L.T("\u00BFNo tienes casa o qu\u00E9?"), L.T("Ni yo trabajo a estas horas."), L.T("Esto ya son horas extra.") });
            }
            return By(s.MascotPersonality,
                new string[] { L.T("\u00A1Hola! Sigo por aqu\u00ED."), L.T("\u00BFNecesitas una captura?"), L.T("Voy a dar una vuelta."), L.T("\u00A1Qu\u00E9 buen d\u00EDa para capturar!"), L.T("\u00BFMe ense\u00F1as lo que haces?"),
                               L.T("Estoy aqu\u00ED por si me necesitas."), L.T("Bip bup, todo en orden."), L.T("\u00BFUna captura r\u00E1pida?"), L.T("Me gusta tu escritorio."), L.T("Si quieres, grabo un v\u00EDdeo.") },
                new string[] { L.T("Qu\u00E9 paz\u2026"), L.T("Sigo aqu\u00ED."), L.T("Un paseo corto y vuelvo."), L.T("Todo tranquilo."), L.T("Se est\u00E1 bien aqu\u00ED."), L.T("Sin prisa, sin pausa."),
                               L.T("Disfruto del silencio."), L.T("Respira hondo."), L.T("Los buenos momentos no hacen ruido.") },
                new string[] { L.T("\u00BFTrabajando o mirando memes?"), L.T("Me aburro\u2026 \u00BFcapturamos algo?"), L.T("Ojo, que te vigilo."), L.T("\u00BFEse es tu mejor escritorio?"), L.T("Me debes una captura."),
                               L.T("Yo no digo nada, pero\u2026"), L.T("Tienes demasiadas pesta\u00F1as abiertas."), L.T("Esa ventana la he visto antes."), L.T("Si me ignoras, hago cosas raras."), L.T("Hoy me siento fotog\u00E9nico.") });
        }

        // What it says when it starts doing something with its props (null: nothing this time).
        public static string ForAct(int personality, int act)
        {
            if (rnd.NextDouble() < 0.45) return null;
            switch (act)
            {
                case MascotProps.Coffee:
                    return By(personality, new string[] { L.T("\u00A1Un caf\u00E9!"), L.T("Pausa para el caf\u00E9."), L.T("Mmm, qu\u00E9 rico.") }, new string[] { L.T("Un cafecito con calma."), L.T("Sorbo a sorbo."), L.T("Qu\u00E9 aroma.") }, new string[] { L.T("Ni se te ocurra pedirme."), L.T("Caf\u00E9 para m\u00ED, no para ti."), L.T("Mi quinto de hoy.") });
                case MascotProps.Juggle:
                    return By(personality, new string[] { L.T("\u00A1Mira, mira!"), L.T("\u00A1Tres a la vez!"), L.T("\u00A1Sin manos! Bueno, con ellas.") }, new string[] { L.T("Un poco de pr\u00E1ctica."), L.T("El ritmo es lo que cuenta."), L.T("Concentraci\u00F3n.") }, new string[] { L.T("\u00BFA que no puedes t\u00FA?"), L.T("Cobro entrada, eh."), L.T("Ojo, que se me caen.") });
                case MascotProps.Console:
                    return By(personality, new string[] { L.T("\u00A1Un ratito de juego!"), L.T("\u00A1Nuevo r\u00E9cord!"), L.T("Una partidita r\u00E1pida.") }, new string[] { L.T("Un nivelito y ya."), L.T("Juego tranquilo."), L.T("Sin prisas, sin vidas.") }, new string[] { L.T("Ni me hables, voy ganando."), L.T("\u00A1Cuarta vida!"), L.T("Esto no es trabajo, es cultura.") });
                case MascotProps.Stretch:
                    return By(personality, new string[] { L.T("\u00A1A estirar!"), L.T("Ufff, qu\u00E9 bien sienta."), L.T("\u00A1Cr\u00E1c, cr\u00E1c!") }, new string[] { L.T("Estirar tambi\u00E9n es descansar."), L.T("Aaah\u2026"), L.T("Un poco de movimiento.") }, new string[] { L.T("Hasta los bots se anquilosan."), L.T("Mira qu\u00E9 flexibilidad."), L.T("Tanto estar sentado\u2026") });
                case MascotProps.Read:
                    return By(personality, new string[] { L.T("\u00A1Qu\u00E9 libro m\u00E1s bueno!"), L.T("Un capitulito."), L.T("\u00A1Mira qu\u00E9 final!") }, new string[] { L.T("Un buen libro y silencio."), L.T("Cap\u00EDtulo a cap\u00EDtulo."), L.T("Leyendo un rato.") }, new string[] { L.T("No hagas spoilers."), L.T("Shhh, que llego al final."), L.T("Mejor que tu escritorio.") });
                case MascotProps.Water:
                    return By(personality, new string[] { L.T("\u00A1Ag\u00FCita fresca!"), L.T("\u00A1A tu salud!"), L.T("Glu, glu, glu.") }, new string[] { L.T("Un poco de agua sienta bien."), L.T("Sorbo a sorbo."), L.T("Hidratarse es cuidarse.") }, new string[] { L.T("Agua, el refresco de los campeones."), L.T("\u00BFBrindamos? T\u00FA tambi\u00E9n."), L.T("Ni caf\u00E9 ni nada: agua.") });
                case MascotProps.EyeRest:
                    return By(personality, new string[] { L.T("\u00A1Qu\u00E9 lejos se ve todo!"), L.T("Miro el horizonte."), L.T("\u00A1All\u00ED a lo lejos!") }, new string[] { L.T("Los ojos tambi\u00E9n descansan."), L.T("Mirar lejos relaja."), L.T("Calma para la vista.") }, new string[] { L.T("Veo, veo\u2026 algo lejano."), L.T("Tu pantalla puede esperar."), L.T("Mira, un p\u00E1jaro. O no.") });
                case MascotProps.Legs:
                    return By(personality, new string[] { L.T("\u00A1Arriba y abajo!"), L.T("\u00A1Una, dos!"), L.T("\u00A1Vamos, piernas!") }, new string[] { L.T("Despacito, sin prisa."), L.T("Mover las piernas sienta bien."), L.T("Arriba\u2026 y abajo.") }, new string[] { L.T("Esto es mejor que el gimnasio."), L.T("No me copies, que te canso."), L.T("\u00A1Mira qu\u00E9 piernas!") });
                case MascotProps.Breathe:
                    return By(personality, new string[] { L.T("Respira conmigo."), L.T("Dentro\u2026 y fuera."), L.T("\u00A1Qu\u00E9 bien se respira!") }, new string[] { L.T("Inspira\u2026 suelta."), L.T("Todo a su ritmo."), L.T("Un poco de calma.") }, new string[] { L.T("Respirar es gratis, aprovecha."), L.T("Hasta un bot necesita aire."), L.T("Inspira hondo, que no cobro.") });
                default:
                    return By(personality, new string[] { L.T("\u00A1Sonr\u00EDe!"), L.T("\u00A1Patata!"), L.T("\u00A1Clic!") }, new string[] { L.T("Un buen recuerdo."), L.T("Luz perfecta."), L.T("Quieto un segundo.") }, new string[] { L.T("Salgo mejor yo."), L.T("Esta va al marco."), L.T("Di \u00ABwhisky\u00BB.") });
            }
        }

        // ---- Healthy breaks: the reminder, what it says while you do it, and the thanks.

        public static string Remind(Settings s, int kind, int minutes)
        {
            string t = minutes >= 60 ? (minutes % 60 == 0 ? (minutes / 60 == 1 ? L.T("una hora") : Count(minutes / 60, "{0} horas", "{0} horas")) : L.T("hora y media")) : Count(minutes, "{0} minuto", "{0} minutos");
            switch (kind)
            {
                case WellKind.Water:
                    return By(s.MascotPersonality,
                        new string[] { L.F("\u00A1Hora de beber agua! Llevas {0} sin un trago.", t), L.T("\u00BFUn vaso de agua? Yo me tomo uno contigo."), L.T("\u00A1Ag\u00FCita! Tu cuerpo te lo agradecer\u00E1.") },
                        new string[] { L.T("Un poco de agua te sentar\u00E1 bien."), L.F("Llevas {0} trabajando. \u00BFBebemos agua?", t), L.T("Un vaso de agua y seguimos.") },
                        new string[] { L.F("\u00BFAgua? \u00BFTe suena? Llevas {0}.", t), L.T("Si no bebes agua, me la bebo yo."), L.T("Plantita que no se riega, se seca.") });
                case WellKind.Eyes:
                    return By(s.MascotPersonality,
                        new string[] { L.T("\u00A1Descansa la vista! Mira algo lejano 20 segundos."), L.F("Tus ojos llevan {0} pegados a la pantalla. \u00BFMiramos lejos?", t), L.T("Regla 20-20-20: mira a 6 metros durante 20 segundos.") },
                        new string[] { L.T("Deja que tus ojos descansen: mira a lo lejos un momento."), L.T("20 segundos mirando lejos. Sin prisa."), L.T("Tus ojos merecen una pausa.") },
                        new string[] { L.T("Esa pantalla no se va a escapar. Mira por la ventana."), L.T("Parpadea, que se te olvida."), L.T("Tus ojos piden vacaciones. Veinte segundos.") });
                case WellKind.Move:
                    return By(s.MascotPersonality,
                        new string[] { L.F("\u00A1Arriba! Llevas {0} en la silla. \u00A1Estiramos las piernas!", t), L.T("\u00A1Hora de moverse! Lev\u00E1ntate un momento."), L.T("\u00BFDamos un pase\u00EDto? Tus piernas lo piden.") },
                        new string[] { L.T("Lev\u00E1ntate y estira un poco el cuerpo."), L.T("Un minuto de pie sienta de maravilla."), L.F("Llevas {0} en la silla. Mu\u00E9vete un poco.", t) },
                        new string[] { L.T("Tu silla te va a cobrar alquiler."), L.T("Si no te levantas, me pongo a bailar."), L.T("\u00BFTe has pegado a la silla? Arriba.") });
                default:
                    return By(s.MascotPersonality,
                        new string[] { L.T("\u00BFRespiramos juntos un minuto?"), L.T("Una pausa para respirar te va a encantar."), L.T("Seis respiraciones lentas y como nuevo.") },
                        new string[] { L.T("Respira hondo conmigo un momento."), L.T("Un minuto de calma: respiremos."), L.T("Inspira, suelta. As\u00ED de simple.") },
                        new string[] { L.T("Respira, que se te nota el estr\u00E9s."), L.T("Un minuto sin correos. Respira."), L.T("Hasta los bots respiramos. Bueno, casi.") });
            }
        }

        public static string WellDone(Settings s, int kind, int today)
        {
            return By(s.MascotPersonality, new string[] { L.T("\u00A1Genial!"), L.T("\u00A1Muy bien!"), L.T("\u00A1As\u00ED me gusta!") }, new string[] { L.T("Bien hecho."), L.T("Perfecto."), L.T("Qu\u00E9 bien.") },
                      new string[] { L.T("Mira t\u00FA qu\u00E9 disciplina."), L.T("No est\u00E1 mal."), L.T("Ole.") }) + " " + WellCount(kind, today, s.WaterGoal);
        }

        // How many of that break today, said plainly (also when the mascot doesn't chat).
        public static string WellCount(int kind, int today) { return WellCount(kind, today, 0); }

        // waterGoal: the glasses a day aimed at (0 none), said with the water count.
        public static string WellCount(int kind, int today, int waterGoal)
        {
            if (kind == WellKind.Water && waterGoal > 0)
                return today >= waterGoal ? (today == waterGoal ? L.F("\u00A1Objetivo de agua cumplido: {0}!", Count(waterGoal, "{0} vaso", "{0} vasos")) : L.F("Ya van {0} hoy: objetivo cumplido.", Count(today, "{0} vaso", "{0} vasos")))
                     : today == 1 ? L.F("Tu primer vaso de hoy (de {0}).", waterGoal) : L.F("Ya van {0} de {1} vasos hoy.", today, waterGoal);
            return kind == WellKind.Water ? (today == 1 ? L.T("Tu primer vaso de hoy.") : L.F("Ya van {0} hoy.", Count(today, "{0} vaso", "{0} vasos")))
                 : kind == WellKind.Eyes ? (today == 1 ? L.T("Primer descanso de vista de hoy.") : L.F("Hoy: {0}.", Count(today, "{0} descanso de vista", "{0} descansos de vista")))
                 : kind == WellKind.Move ? (today == 1 ? L.T("Primera pausa para moverte.") : L.F("Hoy: {0}.", Count(today, "{0} pausa para moverte", "{0} pausas para moverte")))
                 : (today == 1 ? L.T("Primera respiraci\u00F3n de hoy.") : L.F("Hoy: {0}.", Count(today, "{0} respiraci\u00F3n", "{0} respiraciones")));
        }

        // Answers to "Recu\u00E9rdamelo en 10 min" and "Hoy no": they always say what was decided, chatty or not.
        public static string WellLater(Settings s)
        {
            if (!s.MascotTalks) return L.T("Te lo recuerdo en 10 minutos.");
            return By(s.MascotPersonality, new string[] { L.T("\u00A1Vale! Te aviso en 10 minutos."), L.T("\u00A1De acuerdo! En 10 minutos te lo recuerdo.") }, new string[] { L.T("Sin problema: en 10 minutos te aviso."), L.T("Te lo recuerdo en 10 minutos.") },
                      new string[] { L.T("Vale, 10 minutos y no te escapas."), L.T("En 10 minutos vuelvo, \u00BFeh?") });
        }

        public static string WellToday(Settings s, int kind)
        {
            string what = kind == WellKind.Water ? L.T("lo del agua") : kind == WellKind.Eyes ? L.T("lo de la vista") : kind == WellKind.Move ? L.T("lo de moverte") : L.T("lo de respirar");
            if (!s.MascotTalks) return L.F("Hoy no te recuerdo m\u00E1s {0}.", what);
            return By(s.MascotPersonality, new string[] { L.F("\u00A1Vale! Hoy no te recuerdo m\u00E1s {0}.", what) }, new string[] { L.F("De acuerdo, por hoy no te recuerdo m\u00E1s {0}.", what) },
                      new string[] { L.F("Vale, hoy te libras de {0}.", what) });
        }

        // What it says when told to do something (-1: woken up on request).
        public static string ForCmd(int personality, int cmd)
        {
            switch (cmd)
            {
                case -1: return By(personality, new string[] { L.T("\u00A1Ya estoy despierto!"), L.T("\u00A1Buenos d\u00EDas otra vez!") }, new string[] { L.T("Mmm\u2026 ya voy."), L.T("Un ratito m\u00E1s y me levanto.") }, new string[] { L.T("\u00A1Eh! Estaba so\u00F1ando algo genial."), L.T("\u00BFYa? Qu\u00E9 pesado.") });
                case MascotCmd.Play: return By(personality, new string[] { L.T("\u00A1A jugar!"), L.T("\u00A1Siii\u00ED, juguemos!"), L.T("\u00A1Qu\u00E9 ganas ten\u00EDa!") }, new string[] { L.T("Un ratito de juego, vale."), L.T("Jugamos sin prisa.") }, new string[] { L.T("\u00A1Ahora ver\u00E1s!"), L.T("Te reto a ver qui\u00E9n aguanta m\u00E1s.") });
                case MascotCmd.Juggle: return By(personality, new string[] { L.T("\u00A1Mira, mira!"), L.T("\u00A1Tres bolas! \u00A1All\u00E1 voy!") }, new string[] { L.T("Con ritmo y sin prisa."), L.T("A ver si no se cae ninguna.") }, new string[] { L.T("Esto lo hago con los ojos cerrados."), L.T("No te despistes, que vuelan.") });
                case MascotCmd.Dance: return By(personality, new string[] { L.T("\u00A1M\u00FAsica, maestro!"), L.T("\u00A1A bailar!") }, new string[] { L.T("Un bailecito suave."), L.T("Muevo el esqueleto despacito.") }, new string[] { L.T("Mira estos pasos."), L.T("Tiembla, Fred Astaire.") });
                case MascotCmd.Sleep: return By(personality, new string[] { L.T("Aaah\u2026 hasta luego."), L.T("\u00A1Buenas noches!") }, new string[] { L.T("Qu\u00E9 sue\u00F1ecito\u2026 zzz."), L.T("Cierro los ojos un rato.") }, new string[] { L.T("Despi\u00E9rtame si hay churros."), L.T("Ni se te ocurra tocarme.") });
                case MascotCmd.Jump: return By(personality, new string[] { L.T("\u00A1Hop, hop, hop!"), L.T("\u00A1Arriba!") }, new string[] { L.T("Un saltito, nada m\u00E1s."), L.T("Hop. Hop. Hop.") }, new string[] { L.T("\u00BFM\u00E1s alto? \u00A1Mira!"), L.T("Esto es f\u00E1cil.") });
                case MascotCmd.Twirl: return By(personality, new string[] { L.T("\u00A1Weee!"), L.T("\u00A1Ta-ch\u00E1n!") }, new string[] { L.T("Una vueltecita."), L.T("Giro con calma.") }, new string[] { L.T("\u00A1Y sin marearme!"), L.T("Aplauso, por favor.") });
                case MascotCmd.Wave: return By(personality, new string[] { L.T("\u00A1Hola, hola!"), L.T("\u00A1Buenas!") }, new string[] { L.T("Hola, qu\u00E9 tal."), L.T("Un saludo tranquilo.") }, new string[] { L.T("S\u00ED, te he visto."), L.T("\u00BFQu\u00E9 pasa, jefe?") });
                case MascotCmd.Flip: return By(personality, new string[] { L.T("\u00A1Voltereta!"), L.T("\u00A1Mortal atr\u00E1s!") }, new string[] { L.T("Ah\u00ED va, con cuidado."), L.T("Una voltereta suave.") }, new string[] { L.T("\u00BFEso sabe hacerlo tu rat\u00F3n?"), L.T("Diez de diez.") });
                case MascotCmd.Bow: return By(personality, new string[] { L.T("Muchas gracias, muchas gracias."), L.T("\u00A1El p\u00FAblico me adora!") }, new string[] { L.T("Con todos mis respetos."), L.T("Un gesto cort\u00E9s.") }, new string[] { L.T("Hagan cola para el aut\u00F3grafo."), L.T("Gracias, soy yo.") });
                case MascotCmd.Cheer: return By(personality, new string[] { L.T("\u00A1Hurra!"), L.T("\u00A1Fiesta, fiesta!"), L.T("\u00A1Lo has conseguido!") }, new string[] { L.T("Muy bien hecho."), L.T("Hay motivos para sonre\u00EDr.") }, new string[] { L.T("\u00A1Eso es! \u00A1M\u00E1s confeti!"), L.T("\u00A1Que suene la bocina!") });
                case MascotCmd.Love: return By(personality, new string[] { L.T("\u00A1Te quiero un mont\u00F3n!"), L.T("\u00A1Qu\u00E9 bonito!") }, new string[] { L.T("Qu\u00E9 gusto."), L.T("Gracias por estar.") }, new string[] { L.T("Vale, un poquito s\u00ED."), L.T("No se lo cuentes a nadie.") });
                case MascotCmd.Bubbles: return By(personality, new string[] { L.T("\u00A1Pompas!"), L.T("\u00A1Mira c\u00F3mo brillan!") }, new string[] { L.T("Pompas y silencio."), L.T("Qu\u00E9 relajante.") }, new string[] { L.T("No las explotes t\u00FA."), L.T("Esta es la m\u00E1s grande.") });
                default: return null;
            }
        }

        // Reactions to being moved around.
        public static string Fall(Settings s)
        {
            return By(s.MascotPersonality, new string[] { L.T("\u00A1Aaaah!"), L.T("\u00A1Cuidado!"), L.T("\u00A1Que me caigo!"), L.T("\u00A1Socorro!") }, new string[] { L.T("Vaya\u2026"), L.T("Ay, ay, ay."), L.T("Qu\u00E9 remedio.") }, new string[] { L.T("\u00A1Me han quitado el suelo!"), L.T("\u00A1Esto es culpa tuya!"), L.T("\u00A1Mis cosas!") });
        }

        public static string Drag(Settings s)
        {
            return By(s.MascotPersonality, new string[] { L.T("\u00A1Uoooh!"), L.T("\u00A1Vamos all\u00E1!"), L.T("\u00A1Qu\u00E9 vistas!"), L.T("\u00A1Wiii!") }, new string[] { L.T("Con suavidad, por favor."), L.T("Bien, bien."), L.T("Despacito.") }, new string[] { L.T("\u00A1Eh, sin manos!"), L.T("\u00BFA d\u00F3nde me llevas?"), L.T("Soy delicado, \u00BFeh?") });
        }

        public static string Land(Settings s)
        {
            return By(s.MascotPersonality, new string[] { L.T("\u00A1Aterrizaje perfecto!"), L.T("\u00A1Ta-ch\u00E1n!"), L.T("\u00A1Tres puntos!") }, new string[] { L.T("Mejor aqu\u00ED."), L.T("Firme otra vez."), L.T("Todo en su sitio.") }, new string[] { L.T("Diez de diez."), L.T("Eso lo tengo controlado."), L.T("Ni un rasgu\u00F1o.") });
        }

        public static string Climb(Settings s)
        {
            return By(s.MascotPersonality, new string[] { L.T("\u00A1Hop!"), L.T("\u00A1Desde aqu\u00ED se ve todo!"), L.T("\u00A1Arriba!") }, new string[] { L.T("Buen sitio para pensar."), L.T("Un descansito aqu\u00ED."), L.T("Qu\u00E9 vistas.") }, new string[] { L.T("Ahora soy el rey de la ventana."), L.T("Qu\u00E9 desorden hay aqu\u00ED abajo."), L.T("Esta ventana es m\u00EDa.") });
        }

        public static string LevelUp(Settings s, int level)
        {
            return L.F("\u00A1Ya somos \u00AB{0}\u00BB!", MascotParts.LevelNames[level].ToLowerInvariant());
        }
    }
}
