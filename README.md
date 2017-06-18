# TextSearcher.NET

## Задание
Разработать приложение, которое должно иметь возможность предоставить пользователю указать диск или каталог, маску файлов и текст, 
который требуется найти в этих файлах, а также удобный интерфейс для обработки результатов поиска.

## Реализация
* IDE: MSVS 2015
* LANG: C#/SQL, WPF

## Описание приложения
Программа предоставляет возможность поиска текста на жестком диске напрямую или в БД, 
если этот поиск уже производился. В настройки поиска включен учёт регистра и пропуск файлов с размером больше указанного.
Найденные файлы формируются в список, который отображается в главном окне программы. 
Предоставляется возможность по выбору из списка открывать, копировать, перемещать или удалять файлы.

## Руководство пользователя / Скриншоты

Главное окно программы
![ScreenShot](https://raw.github.com/insendend/TextSearcher.NET/master/hmTextSearcher/Screenshots/scrn1.jpg)

После выбора всех параметров и нажатии кнопки "Search" предоставляется возможность режима поиска:
###### на жестком диске
![ScreenShot](https://raw.github.com/insendend/TextSearcher.NET/master/hmTextSearcher/Screenshots/scrn2.jpg)
###### в БД
![ScreenShot](https://raw.github.com/insendend/TextSearcher.NET/master/hmTextSearcher/Screenshots/scrn3.jpg)

При выборе "поиска в БД" нужно также выбрать строку соединения с БД из списка или сформировать свою:
![ScreenShot](https://raw.github.com/insendend/TextSearcher.NET/master/hmTextSearcher/Screenshots/scrn4.jpg)
![ScreenShot](https://raw.github.com/insendend/TextSearcher.NET/master/hmTextSearcher/Screenshots/scrn5.jpg)

После выбора всех параметров поиска можно стартовать
![ScreenShot](https://raw.github.com/insendend/TextSearcher.NET/master/hmTextSearcher/Screenshots/scrn6.jpg)
