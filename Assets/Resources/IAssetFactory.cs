using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//工厂模式
interface IAssetFactory
{
    GameObject LoadAnimal(string name);
    GameObject LoadFood(string name);
    AudioClip LoadSound(string name);
    Sprite LoadImg(string name);

}
