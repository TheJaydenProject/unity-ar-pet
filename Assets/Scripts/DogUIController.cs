using UnityEngine;

public class DogUIController : MonoBehaviour
{
    public DogStats dog;

    public void OnTrainButton() 
    {
        dog.Train();
    }

    public void OnPlayButton()
    {
        dog.Roll();
    }

    public void OnRelaxButton()
    {
        dog.Rest();
    }
}
