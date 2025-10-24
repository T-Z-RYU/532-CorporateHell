using UnityEngine;
using UnityEngine.EventSystems;

public class MouseInteract : MonoBehaviour
{

    Vector3 mousePosition;
    RaycastHit2D raycastHit2D;
    Transform clickObject;
    Transform prevHoverObject, nextHoverObject,UIInteractObject;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		/*if(EventSystem.current.IsPointerOverGameObject()) return;
        mousePosition = Input.mousePosition;
        Ray mouseRay = Camera.main.ScreenPointToRay(mousePosition);

        prevHoverObject = nextHoverObject;

        raycastHit2D = Physics2D.Raycast(mouseRay.origin, mouseRay.direction);
        nextHoverObject = raycastHit2D ? raycastHit2D.collider.transform : null;

        if(nextHoverObject)
        {

            if(nextHoverObject.tag == "UIObject")
            {
                UIInteractObject = nextHoverObject;

                nextHoverObject.gameObject.GetComponent<CanInteract>().StartMouseHover();
                if(Input.GetMouseButtonDown( 0 ))
                {
                    nextHoverObject.GetComponent<CanInteract>().InteractWithUI();

                }
            }
            if(prevHoverObject && nextHoverObject && prevHoverObject.GetInstanceID() != nextHoverObject.GetInstanceID())
            {
                UIInteractObject.GetComponent<CanInteract>().StopMouseHover();

            }
        } else
        {
            if(prevHoverObject)
            {
                nextHoverObject.GetComponent<CanInteract>().StopMouseHover();

            }
        }
        */
	}
}
