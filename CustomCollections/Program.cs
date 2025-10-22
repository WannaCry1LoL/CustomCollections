// See https://aka.ms/new-console-template for more information


using CustomCollections.Collections;

MyLinkedList<int> a = new();
a.Add(1);
a.Add(2);
a.Add(3);
Console.WriteLine(a.IndexOf(2));