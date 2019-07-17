﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TextLogControl : MonoBehaviour
{
    [SerializeField]
    private GameObject textTemplate;

    private List<GameObject> textItems;

    private void Start()
    {
        textItems = new List<GameObject>();
    }

    public void LogText(string newTextString, Color newColour)
    {
        if (textItems.Count == 10)
        {
            GameObject tempItem = textItems[0];
            Destroy(tempItem.gameObject);
            textItems.Remove(tempItem);
        }

        GameObject newText = Instantiate(textTemplate) as GameObject;
        newText.SetActive(true);

        newText.GetComponent<TextLogItem>().SetText(newTextString, newColour);
        newText.transform.SetParent(textTemplate.transform.parent, false);

        textItems.Add(newText.gameObject);
    }
}