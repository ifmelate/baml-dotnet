class Person {
    name string
    age int
}

function GetPerson(id: string) -> Person {
    client "test/model"
    prompt #"Get person with id {{ id }}"#
}

