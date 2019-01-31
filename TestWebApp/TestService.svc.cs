using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
Разработать web-service api:
1)      Язык серверной части = C#
2)      Методы:
localhost/api/post
localhost/api/get/{id}
localhost/api/patch{id}
3)      Метод post передает данные в формате json:
“id”:”int”
“name”: “Ivan”
“surname”: “Ivanov”
4)      Данные сохраняются в глобальных переменных сервера(т.е.не надо использовать БД, время жизни данных заканчивается с окончанием работы сервера)
5)      Данные на сервере не должны иметь одинаковых имен.
6)      В метод get и patch передается id пользователя и выводится(записывается) информация о пользователе также в формате json
*/

/*
 * !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 * В данной реализации значения id тоже должны ьыть уникальными
*/

/*
 * !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 * Запросы для тестирования:
 1) GET: http://localhost:50859/TestService.svc/test - выведет в JSON-формате все имеющиеся записи
 2) GET: http://localhost:50859/TestService.svc/test/id (где id - целое число) - выведет данные записи, у которой id равен переданному id (если такакя запись есть)
 3) POST: http://localhost:50859/TestService.svc/test c POST-параметром в JSON-формате {"id": "x", "name":"y", "surname":"z"}, где x - целое число, y - строка, z - строка...
 ... добавляет новую запись с переданными парметрами, при условии, что еще нет записи с переданным id и сочетание name:surname уникально.
 4) PATCH: http://localhost:50859/TestService.svc/test/id (где id - целое число) c POST-параметром в JSON-формате {"name":"y", "surname":"z"} (name и surname не являются обязательными параметрами), ...
 ... можно передать {"name":"y"} или {"surname":"z"}, где y - строка, z - строка...
 ... обновляет данные у переданного id (если такой есть). При этом сочетание новых значений name:surname должно оставаться уникальным. Если меняется имя, ...
 ... но уже есть другая запись с этим новым именем и текущей фамилией, то обновление имени не произойдет.
*/


namespace TestWebApp
{
    // Класс для хранения данных
    class classData
    {
        public int id;
        public string name;
        public string surname;

        public classData(int _id, string _name, string _surname)
        {
            id = _id;
            name = _name;
            surname = _surname;
        }
    }



    [ServiceContract(Namespace = "")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class TestService
    {
        // Чтобы использовать протокол HTTP GET, добавьте атрибут [WebGet]. (По умолчанию ResponseFormat имеет значение WebMessageFormat.Json.)
        // Чтобы создать операцию, возвращающую XML,
        //     добавьте [WebGet(ResponseFormat=WebMessageFormat.Xml)]
        //     и включите следующую строку в текст операции:
        //         WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
        [OperationContract]
        public void DoWork()
        {
            // Добавьте здесь реализацию операции
            return;
        }

        // Добавьте здесь дополнительные операции и отметьте их атрибутом [OperationContract]

        

        // ---------------------------------------------------------------------------------------------------
        // Список для хранения данных
        static List<classData> listData = new List<classData>();
        

        // Преобразование JSON-строки в объект
        JToken getJsonData(string s)
        {
            try
            {
                return JToken.Parse(s);
            }
            catch (JsonReaderException ex)
            {
                return null;
            }
        }


        // Проверка уникальности значения id
        bool checkIfExistIdInList(int id)
        {
            foreach(classData one in listData)
            {
                if (one.id == id)
                    return true;
            }

            return false;
        }


        // Проверка уникальности сочетания name:surname
        bool checkIfExistNameSurnameInList(string name, string surname)
        {
            foreach (classData one in listData)
            {
                if (one.name.Equals(name) && one.surname.Equals(surname))
                    return true;
            }

            return false;
        }



        // GET-метод, выводящий все записи
        [WebGet(UriTemplate = "/test", ResponseFormat = WebMessageFormat.Json)]
        public string getAllData()
        {
            return JsonConvert.SerializeObject(listData);
        }


        // GET-метод, выводящий данные для записи с указанным id
        [WebGet(UriTemplate = "/test/{id}", ResponseFormat = WebMessageFormat.Json)]
        public string getDataOnId(string id)
        {
            string returnStr = "[]";
            int idInt = 0;

            // id передано в запросе
            if (id != null)
            {
                // id не пустое
                if (id.Length > 0)
                {
                    // id - это число
                    if (Int32.TryParse(id.ToString(), out idInt))
                    {
                        // Нахожу в списке нужный id. Если такой есть - отдаю данные по нему
                        foreach (classData one in listData)
                        {
                            if (one.id == idInt)
                                return JsonConvert.SerializeObject(one);
                        }
                    }
                    else
                        returnStr = "{\"error\":\"true\", \"text\":\"Field [id] must be INT type\"}";
                }
                else
                    returnStr = "{\"error\":\"true\", \"text\":\"Empty values for required field: [id]\"}";
            }
            else
                returnStr = "{\"error\":\"true\", \"text\":\"Not exist required field: [id]\"}";


            return returnStr;
        }


        // POST-метод для добавления новых данных
        [WebInvoke(Method = "POST", UriTemplate = "/test", RequestFormat = WebMessageFormat.Json,
                    ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string addData(string id, string name, string surname)
        {
            string returnStr = "[]";
            int idInt = 0;
            
            // Переданы все необходимые значения
            if ((id != null) && (name != null) && (surname != null))
            {
                // Значения не пустые
                if ((id.Length > 0) && (name.Length > 0) && (surname.Length > 0))
                {
                    // id - это число
                    if (Int32.TryParse(id.ToString(), out idInt))
                    {
                        // Если в списке нет такого id и сочетание name:surname уникально, то добавляю данные в список
                        if (!checkIfExistIdInList(idInt) && !checkIfExistNameSurnameInList(name, surname))
                            listData.Add(new classData(idInt, name, surname));
                        else
                            returnStr = "{\"error\":\"true\", \"text\":\"Duplicate data for [id] or [[name] and [surname]]\"}";
                    }
                    else
                        returnStr = "{\"error\":\"true\", \"text\":\"Field [id] must be INT type\"}";
                }
                else
                    returnStr = "{\"error\":\"true\", \"text\":\"Empty values for required fields: [id], [name], [surname]\"}";
            }
            else
                returnStr = "{\"error\":\"true\", \"text\":\"Not exist required fields: [id], [name], [surname]\"}";


            return returnStr;
        }


        // PATCH-метод для обновления данных
        [WebInvoke(Method = "PATCH", UriTemplate = "/test/{id}", RequestFormat = WebMessageFormat.Json,
                    ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.Wrapped)]
        public string patchData(string id, string name, string surname)
        {
            string returnStr = "[]";
            int idInt = 0;
            string tmpName = "";
            string tmpSurname = "";

            // id передано в запросе
            if (id != null)
            {
                // id не пустое
                if (id.Length > 0)
                {
                    // id - это число
                    if (Int32.TryParse(id.ToString(), out idInt))
                    {
                        // Перебираю список
                        for (int cnt = 0; cnt < listData.Count; cnt++)
                        {
                            // Найден нужный id
                            if (listData[cnt].id == idInt)
                            {
                                // Если передано name, то беру его, иначе - беру имеющееся значение
                                if ((name != null) && (name.Length > 0))
                                    tmpName = name;
                                else
                                    tmpName = listData[cnt].name;

                                // Если передано surname, то беру его, иначе - беру имеющееся значение
                                if ((surname != null) && (surname.Length > 0))
                                    tmpSurname = surname;
                                else
                                    tmpSurname = listData[cnt].surname;

                                // Проверяю уникальность нового сочетания name:surname. Если уникально, то обновляю данные
                                if (!checkIfExistNameSurnameInList(tmpName, tmpSurname))
                                {
                                    listData[cnt].name = tmpName;
                                    listData[cnt].surname = tmpSurname;
                                }
                                else
                                    returnStr = "{\"error\":\"true\", \"text\":\"Duplicate data for [[name] and [surname]]\"}";
                            }
                        }
                    }
                    else
                        returnStr = "{\"error\":\"true\", \"text\":\"Field [id] must be INT type\"}";
                }
                else
                    returnStr = "{\"error\":\"true\", \"text\":\"Empty values for required field: [id]\"}";
            }
            else
                returnStr = "{\"error\":\"true\", \"text\":\"Not exist required field: [id]\"}";


            return returnStr;
        }
        // ---------------------------------------------------------------------------------------------------

    }
}
